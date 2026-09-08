using System.Windows.Input;
using PasswordSave.Common;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.ViewModels;

public sealed class OnboardingViewModel : ObservableObject
{
    private readonly IKeyDerivationService _keyDerivationService;
    private readonly IVaultKeyService _vaultKeyService;
    private readonly IVaultMetadataStore _vaultMetadataStore;
    private readonly IAppLockService _appLockService;
    private readonly IVaultSessionService _vaultSessionService;

    private string _masterPassword = string.Empty;
    private string _confirmMasterPassword = string.Empty;
    private string _pin = string.Empty;
    private string _confirmPin = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    public event EventHandler? VaultCreated;

    public OnboardingViewModel(
        IKeyDerivationService keyDerivationService,
        IVaultKeyService vaultKeyService,
        IVaultMetadataStore vaultMetadataStore,
        IAppLockService appLockService,
        IVaultSessionService vaultSessionService)
    {
        _keyDerivationService = keyDerivationService;
        _vaultKeyService = vaultKeyService;
        _vaultMetadataStore = vaultMetadataStore;
        _appLockService = appLockService;
        _vaultSessionService = vaultSessionService;

        CreateVaultCommand = new AsyncRelayCommand(CreateVaultAsync, () => !IsBusy);
    }

    public string MasterPassword
    {
        get => _masterPassword;
        set => SetProperty(ref _masterPassword, value);
    }

    public string ConfirmMasterPassword
    {
        get => _confirmMasterPassword;
        set => SetProperty(ref _confirmMasterPassword, value);
    }

    public string Pin
    {
        get => _pin;
        set => SetProperty(ref _pin, value);
    }

    public string ConfirmPin
    {
        get => _confirmPin;
        set => SetProperty(ref _confirmPin, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                ((AsyncRelayCommand)CreateVaultCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand CreateVaultCommand { get; }

    private async Task CreateVaultAsync()
    {
        ErrorMessage = null;

        if (MasterPassword.Length < 8)
        {
            ErrorMessage = "La contraseña maestra debe tener al menos 8 caracteres.";
            return;
        }

        if (MasterPassword != ConfirmMasterPassword)
        {
            ErrorMessage = "Las contraseñas maestras no coinciden.";
            return;
        }

        if (Pin.Length is < 4 or > 6 || Pin != ConfirmPin)
        {
            ErrorMessage = "El PIN debe tener de 4 a 6 dígitos y coincidir en ambos campos.";
            return;
        }

        IsBusy = true;
        try
        {
            var vaultKey = _vaultKeyService.GenerateVaultKey();
            var kdfParameters = _keyDerivationService.CreateParameters();
            var masterKey = _keyDerivationService.DeriveKey(MasterPassword, kdfParameters);
            var wrappedVaultKey = _vaultKeyService.WrapWithMasterKey(vaultKey, masterKey);

            await _vaultMetadataStore.SaveVaultAsync(kdfParameters, wrappedVaultKey);
            await _appLockService.SetPinAsync(Pin);
            await _vaultKeyService.StoreForQuickUnlockAsync(vaultKey);

            MasterPassword = string.Empty;
            ConfirmMasterPassword = string.Empty;
            Pin = string.Empty;
            ConfirmPin = string.Empty;

            _vaultSessionService.SetUnlocked(vaultKey);
            _appLockService.RecordActivity();

            VaultCreated?.Invoke(this, EventArgs.Empty);
        }
        catch (InvalidOperationException ex)
        {
            // p. ej. "ya existe un vault" si el usuario llegó dos veces a esta pantalla.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
