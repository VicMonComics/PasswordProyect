using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordSave.Common;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IAppLockService _appLockService;
    private readonly IVaultKeyService _vaultKeyService;
    private readonly IVaultMetadataStore _vaultMetadataStore;
    private readonly IKeyDerivationService _keyDerivationService;
    private readonly IPremiumStatusService _premiumStatusService;

    private string _currentMasterPassword = string.Empty;
    private string _newMasterPassword = string.Empty;
    private string _confirmNewMasterPassword = string.Empty;
    private string? _masterPasswordMessage;
    private bool _masterPasswordMessageIsError;

    private string _currentPin = string.Empty;
    private string _newPin = string.Empty;
    private string _confirmNewPin = string.Empty;
    private string? _pinMessage;
    private bool _pinMessageIsError;

    private bool _isPremium;
    private int _maxInactivityMinutes = 5;
    private int _inactivityTimeoutMinutes = 5;

    private bool _isBusy;

    public SettingsViewModel(
        IAppLockService appLockService,
        IVaultKeyService vaultKeyService,
        IVaultMetadataStore vaultMetadataStore,
        IKeyDerivationService keyDerivationService,
        IPremiumStatusService premiumStatusService)
    {
        _appLockService = appLockService;
        _vaultKeyService = vaultKeyService;
        _vaultMetadataStore = vaultMetadataStore;
        _keyDerivationService = keyDerivationService;
        _premiumStatusService = premiumStatusService;

        ChangeMasterPasswordCommand = new AsyncRelayCommand(ChangeMasterPasswordAsync, () => !IsBusy);
        ChangePinCommand = new AsyncRelayCommand(ChangePinAsync, () => !IsBusy);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync, () => !IsBusy);
    }

    public string CurrentMasterPassword
    {
        get => _currentMasterPassword;
        set => SetProperty(ref _currentMasterPassword, value);
    }

    public string NewMasterPassword
    {
        get => _newMasterPassword;
        set => SetProperty(ref _newMasterPassword, value);
    }

    public string ConfirmNewMasterPassword
    {
        get => _confirmNewMasterPassword;
        set => SetProperty(ref _confirmNewMasterPassword, value);
    }

    public string? MasterPasswordMessage
    {
        get => _masterPasswordMessage;
        private set => SetProperty(ref _masterPasswordMessage, value);
    }

    public bool MasterPasswordMessageIsError
    {
        get => _masterPasswordMessageIsError;
        private set => SetProperty(ref _masterPasswordMessageIsError, value);
    }

    public string CurrentPin
    {
        get => _currentPin;
        set => SetProperty(ref _currentPin, value);
    }

    public string NewPin
    {
        get => _newPin;
        set => SetProperty(ref _newPin, value);
    }

    public string ConfirmNewPin
    {
        get => _confirmNewPin;
        set => SetProperty(ref _confirmNewPin, value);
    }

    public string? PinMessage
    {
        get => _pinMessage;
        private set => SetProperty(ref _pinMessage, value);
    }

    public bool PinMessageIsError
    {
        get => _pinMessageIsError;
        private set => SetProperty(ref _pinMessageIsError, value);
    }

    public bool IsPremium
    {
        get => _isPremium;
        private set => SetProperty(ref _isPremium, value);
    }

    /// <summary>10 si es premium, 5 si no — límite superior del slider.</summary>
    public int MaxInactivityMinutes
    {
        get => _maxInactivityMinutes;
        private set => SetProperty(ref _maxInactivityMinutes, value);
    }

    public int InactivityTimeoutMinutes
    {
        get => _inactivityTimeoutMinutes;
        set
        {
            if (SetProperty(ref _inactivityTimeoutMinutes, value))
            {
                // Persistir el timeout es una operación local instantánea
                // (Preferences), así que no hace falta bloquear la UI con
                // IsBusy por esto — se dispara y sigue.
                _ = _appLockService.SetInactivityTimeoutAsync(TimeSpan.FromMinutes(value), IsPremium);
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((AsyncRelayCommand)ChangeMasterPasswordCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)ChangePinCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)LogoutCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ChangeMasterPasswordCommand { get; }
    public ICommand ChangePinCommand { get; }
    public ICommand LogoutCommand { get; }

    public async Task InitializeAsync()
    {
        IsPremium = await _premiumStatusService.IsPremiumAsync();
        MaxInactivityMinutes = IsPremium ? 10 : 5;

        // Carga el valor actual sin pasar por el setter público (que
        // dispararía una escritura innecesaria a Preferences con el mismo
        // valor que ya está guardado).
        _inactivityTimeoutMinutes = Math.Min((int)_appLockService.InactivityTimeout.TotalMinutes, MaxInactivityMinutes);
        OnPropertyChanged(nameof(InactivityTimeoutMinutes));
    }

    private async Task ChangeMasterPasswordAsync()
    {
        MasterPasswordMessage = null;

        if (string.IsNullOrEmpty(CurrentMasterPassword))
        {
            SetMasterPasswordError("Ingresa tu contraseña maestra actual.");
            return;
        }

        if (NewMasterPassword.Length < 8)
        {
            SetMasterPasswordError("La nueva contraseña debe tener al menos 8 caracteres.");
            return;
        }

        if (NewMasterPassword != ConfirmNewMasterPassword)
        {
            SetMasterPasswordError("Las contraseñas nuevas no coinciden.");
            return;
        }

        IsBusy = true;
        try
        {
            var currentParameters = await _vaultMetadataStore.GetKdfParametersAsync();
            var wrappedVaultKey = await _vaultMetadataStore.GetWrappedVaultKeyAsync();
            var currentMasterKey = _keyDerivationService.DeriveKey(CurrentMasterPassword, currentParameters);

            byte[] vaultKey;
            try
            {
                vaultKey = _vaultKeyService.UnwrapWithMasterKey(wrappedVaultKey, currentMasterKey);
            }
            catch (CryptographicException)
            {
                SetMasterPasswordError("Tu contraseña maestra actual no es correcta.");
                return;
            }

            // La vault key NO cambia — solo se vuelve a envolver con una
            // master key nueva. Por eso cambiar la contraseña maestra no
            // requiere re-cifrar ninguna credencial ya guardada.
            var newParameters = _keyDerivationService.CreateParameters();
            var newMasterKey = _keyDerivationService.DeriveKey(NewMasterPassword, newParameters);
            var newWrappedVaultKey = _vaultKeyService.WrapWithMasterKey(vaultKey, newMasterKey);

            await _vaultMetadataStore.UpdateVaultAsync(newParameters, newWrappedVaultKey);

            CurrentMasterPassword = string.Empty;
            NewMasterPassword = string.Empty;
            ConfirmNewMasterPassword = string.Empty;
            MasterPasswordMessageIsError = false;
            MasterPasswordMessage = "Contraseña maestra actualizada.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangePinAsync()
    {
        PinMessage = null;

        if (NewPin.Length is < 4 or > 6)
        {
            SetPinError("El PIN nuevo debe tener de 4 a 6 dígitos.");
            return;
        }

        if (NewPin != ConfirmNewPin)
        {
            SetPinError("Los PIN nuevos no coinciden.");
            return;
        }

        IsBusy = true;
        try
        {
            var isCurrentPinValid = await _appLockService.VerifyPinAsync(CurrentPin);
            if (!isCurrentPinValid)
            {
                SetPinError("Tu PIN actual no es correcto.");
                return;
            }

            await _appLockService.SetPinAsync(NewPin);

            CurrentPin = string.Empty;
            NewPin = string.Empty;
            ConfirmNewPin = string.Empty;
            PinMessageIsError = false;
            PinMessage = "PIN actualizado.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LogoutAsync()
    {
        // Reutiliza el mismo mecanismo del bloqueo automático: App.xaml.cs
        // ya está suscrito a IAppLockService.Locked y se encarga de limpiar
        // la sesión y regresar a LoginPage — cerrar sesión manualmente es,
        // en los hechos, forzar ese mismo bloqueo.
        await _appLockService.LockAsync();
    }

    private void SetMasterPasswordError(string message)
    {
        MasterPasswordMessageIsError = true;
        MasterPasswordMessage = message;
    }

    private void SetPinError(string message)
    {
        PinMessageIsError = true;
        PinMessage = message;
    }
}
