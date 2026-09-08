using PasswordSave.Common;
using PasswordSave.Services;
using PasswordSave.Services.Abstractions;
using System.Windows.Input;

namespace PasswordSave.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private readonly IAppLockService _appLockService;
    private readonly IVaultKeyService _vaultKeyService;
    private readonly IVaultMetadataStore _vaultMetadataStore;
    private readonly IKeyDerivationService _keyDerivationService;
    private readonly IDeviceBiometricService _biometricService;
    private readonly IVaultSessionService _vaultSessionService;

    private string _pin = string.Empty;
    private string _masterPassword = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isMasterPasswordMode;
    private bool _isBiometricAvailable;
    private int _remainingPinAttempts = AppLockService.MaxFailedAttempts;

    public event EventHandler? LoginSucceeded;

    public LoginViewModel(
        IAppLockService appLockService,
        IVaultKeyService vaultKeyService,
        IVaultMetadataStore vaultMetadataStore,
        IKeyDerivationService keyDerivationService,
        IDeviceBiometricService biometricService,
        IVaultSessionService vaultSessionService)
    {
        _appLockService = appLockService;
        _vaultKeyService = vaultKeyService;
        _vaultMetadataStore = vaultMetadataStore;
        _keyDerivationService = keyDerivationService;
        _biometricService = biometricService;
        _vaultSessionService = vaultSessionService;

        SubmitPinCommand = new AsyncRelayCommand(SubmitPinAsync, () => Pin.Length is >= 4 and <= 6 && !IsBusy);
        SubmitMasterPasswordCommand = new AsyncRelayCommand(SubmitMasterPasswordAsync, () => !string.IsNullOrWhiteSpace(MasterPassword) && !IsBusy);
        UseBiometricCommand = new AsyncRelayCommand(TryBiometricLoginAsync, () => IsBiometricAvailable && !IsBusy);
        SwitchToMasterPasswordCommand = new RelayCommand(() => IsMasterPasswordMode = true);
    }

    public string Pin
    {
        get => _pin;
        set
        {
            if (SetProperty(ref _pin, value))
                ((AsyncRelayCommand)SubmitPinCommand).RaiseCanExecuteChanged();
        }
    }

    public string MasterPassword
    {
        get => _masterPassword;
        set
        {
            if (SetProperty(ref _masterPassword, value))
                ((AsyncRelayCommand)SubmitMasterPasswordCommand).RaiseCanExecuteChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsMasterPasswordMode
    {
        get => _isMasterPasswordMode;
        private set => SetProperty(ref _isMasterPasswordMode, value);
    }

    public bool IsBiometricAvailable
    {
        get => _isBiometricAvailable;
        private set
        {
            if (SetProperty(ref _isBiometricAvailable, value))
                ((AsyncRelayCommand)UseBiometricCommand).RaiseCanExecuteChanged();
        }
    }

    public int RemainingPinAttempts
    {
        get => _remainingPinAttempts;
        private set => SetProperty(ref _remainingPinAttempts, value);
    }

    public ICommand SubmitPinCommand { get; }
    public ICommand SubmitMasterPasswordCommand { get; }
    public ICommand UseBiometricCommand { get; }
    public ICommand SwitchToMasterPasswordCommand { get; }

    /// <summary>Se llama desde LoginPage.OnAppearing: decide el punto de partida (biometría automática, PIN, o contraseña maestra si ya no hay intentos).</summary>
    public async Task InitializeAsync()
    {
        ErrorMessage = null;
        RemainingPinAttempts = await _appLockService.GetRemainingPinAttemptsAsync();

        if (await _appLockService.IsPinLockedOutAsync())
        {
            IsMasterPasswordMode = true;
            return;
        }

        IsBiometricAvailable = await _biometricService.IsAvailableAsync();

        if (IsBiometricAvailable)
            await TryBiometricLoginAsync();
    }

    private async Task TryBiometricLoginAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var authenticated = await _biometricService.AuthenticateAsync("Desbloquea PasswordSave");
            if (!authenticated)
                return; // el usuario sigue viendo la pantalla de PIN, sin penalizar intentos

            var vaultKey = await _vaultKeyService.TryGetQuickUnlockKeyAsync();
            if (vaultKey is null)
            {
                // No hay copia local para desbloqueo rápido (p. ej. recién
                // restaurado desde backup en un dispositivo nuevo): la
                // biometría autenticó a la persona, pero no puede recuperar
                // la vault key sin la contraseña maestra al menos una vez.
                await _appLockService.NotifyBiometricSuccessAsync();
                IsMasterPasswordMode = true;
                ErrorMessage = "Ingresa tu contraseña maestra para activar el desbloqueo rápido en este dispositivo.";
                return;
            }

            await _appLockService.NotifyBiometricSuccessAsync();
            CompleteLogin(vaultKey);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SubmitPinAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var isValid = await _appLockService.VerifyPinAsync(Pin);
            Pin = string.Empty;

            if (!isValid)
            {
                RemainingPinAttempts = await _appLockService.GetRemainingPinAttemptsAsync();

                if (RemainingPinAttempts <= 0)
                {
                    IsMasterPasswordMode = true;
                    ErrorMessage = "Superaste el número de intentos. Ingresa tu contraseña maestra.";
                }
                else
                {
                    ErrorMessage = $"PIN incorrecto. Te quedan {RemainingPinAttempts} intento(s).";
                }
                return;
            }

            var vaultKey = await _vaultKeyService.TryGetQuickUnlockKeyAsync();
            if (vaultKey is null)
            {
                IsMasterPasswordMode = true;
                ErrorMessage = "Ingresa tu contraseña maestra para activar el desbloqueo rápido en este dispositivo.";
                return;
            }

            CompleteLogin(vaultKey);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SubmitMasterPasswordAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var kdfParameters = await _vaultMetadataStore.GetKdfParametersAsync();
            var wrappedVaultKey = await _vaultMetadataStore.GetWrappedVaultKeyAsync();

            var masterKey = _keyDerivationService.DeriveKey(MasterPassword, kdfParameters);
            MasterPassword = string.Empty;

            byte[] vaultKey;
            try
            {
                vaultKey = _vaultKeyService.UnwrapWithMasterKey(wrappedVaultKey, masterKey);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                ErrorMessage = "Contraseña maestra incorrecta.";
                return;
            }

            // Contraseña maestra correcta: re-habilita PIN/biometría para la
            // próxima vez, guardando de nuevo la copia de desbloqueo rápido.
            await _vaultKeyService.StoreForQuickUnlockAsync(vaultKey);
            await _appLockService.NotifyBiometricSuccessAsync();
            IsMasterPasswordMode = false;

            CompleteLogin(vaultKey);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CompleteLogin(byte[] vaultKey)
    {
        _vaultSessionService.SetUnlocked(vaultKey);
        _appLockService.RecordActivity();
        LoginSucceeded?.Invoke(this, EventArgs.Empty);
    }
}
