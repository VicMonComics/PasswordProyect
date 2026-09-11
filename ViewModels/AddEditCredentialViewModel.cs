using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using PasswordSave.Common;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.ViewModels;

public sealed class AddEditCredentialViewModel : ObservableObject
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPasswordGeneratorService _generatorService;

    // TODO premium: cuando exista la lógica de categorías propias del
    // usuario (solo Premium — ver nota en el área del proyecto), esta lista
    // deja de ser fija y se carga desde un CategoryRepository, más una
    // opción "Crear categoría" visible solo si IPremiumStatusService dice
    // que el usuario es premium.
    public static readonly string[] CategoryOptions = { "General", "Trabajo", "Finanzas", "Familia" };

    private string? _id;
    private string _siteName = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _website = string.Empty;
    private string _notes = string.Empty;
    private string _category = CategoryOptions[0];
    private bool _isFavorite;
    private bool _isPasswordVisible;
    private bool _isEditMode;
    private string _strengthLabel = string.Empty;
    private double _strengthFraction;
    private string? _errorMessage;
    private bool _isBusy;

    public event EventHandler? Closed;

    public AddEditCredentialViewModel(ICredentialRepository credentialRepository, IPasswordGeneratorService generatorService)
    {
        _credentialRepository = credentialRepository;
        _generatorService = generatorService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && IsEditMode);
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
        CopyUsernameCommand = new AsyncRelayCommand(() => CopyAsync(Username));
        CopyPasswordCommand = new AsyncRelayCommand(() => CopyAsync(Password));
        GenerateNewPasswordCommand = new RelayCommand(GenerateNewPassword);
    }

    public string Title => IsEditMode ? "Editar credencial" : "Nueva credencial";
    public string SaveButtonText => IsEditMode ? "Guardar cambios" : "Guardar";
    public string AvatarInitial => SiteName.Length > 0 ? SiteName[..1].ToUpperInvariant() : "?";

    public ObservableCollection<string> CategoryOptionsList { get; } = new(CategoryOptions);

    public string SiteName
    {
        get => _siteName;
        set
        {
            if (SetProperty(ref _siteName, value))
            {
                OnPropertyChanged(nameof(AvatarInitial));
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
                UpdateStrength();
        }
    }

    public string Website
    {
        get => _website;
        set => SetProperty(ref _website, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        private set => SetProperty(ref _isPasswordVisible, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(SaveButtonText));
                ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StrengthLabel
    {
        get => _strengthLabel;
        private set => SetProperty(ref _strengthLabel, value);
    }

    public double StrengthFraction
    {
        get => _strengthFraction;
        private set => SetProperty(ref _strengthFraction, value);
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
            {
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand CopyUsernameCommand { get; }
    public ICommand CopyPasswordCommand { get; }
    public ICommand GenerateNewPasswordCommand { get; }

    /// <summary>Llamar antes de mostrar la página. entryId null = modo "agregar"; con valor = modo "editar".</summary>
    public async Task InitializeAsync(string? entryId, string? prefilledPassword = null)
    {
        _id = entryId;
        IsEditMode = entryId is not null;

        if (entryId is not null)
        {
            var entry = await _credentialRepository.GetByIdAsync(entryId);
            if (entry is not null)
            {
                SiteName = entry.SiteName;
                Username = entry.Username;
                Password = entry.Password;
                Website = entry.Website;
                Notes = entry.Notes;
                Category = entry.Category;
                IsFavorite = entry.IsFavorite;
            }
        }
        else if (!string.IsNullOrEmpty(prefilledPassword))
        {
            Password = prefilledPassword;
        }
    }

    private void GenerateNewPassword()
    {
        var options = new PasswordGenerationOptions(16, UseUppercase: true, UseLowercase: true, UseNumbers: true, UseSymbols: true);
        Password = _generatorService.Generate(options);
    }

    private void UpdateStrength()
    {
        if (string.IsNullOrEmpty(Password))
        {
            StrengthLabel = string.Empty;
            StrengthFraction = 0;
            return;
        }

        // Aproxima la fortaleza a partir del contenido real de la
        // contraseña (no de opciones elegidas): reutiliza el mismo
        // estimador del generador, infiriendo qué tipos de carácter usa.
        var options = new PasswordGenerationOptions(
            Password.Length,
            UseUppercase: Password.Any(char.IsUpper),
            UseLowercase: Password.Any(char.IsLower),
            UseNumbers: Password.Any(char.IsDigit),
            UseSymbols: Password.Any(c => !char.IsLetterOrDigit(c)));

        var strength = _generatorService.EstimateStrength(options);
        (StrengthLabel, StrengthFraction) = strength switch
        {
            PasswordStrength.Debil => ("Débil", 0.25),
            PasswordStrength.Aceptable => ("Aceptable", 0.5),
            PasswordStrength.Segura => ("Segura", 0.75),
            PasswordStrength.MuySegura => ("Muy segura", 1.0),
            _ => (string.Empty, 0.0)
        };
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(SiteName))
        {
            ErrorMessage = "Ponle un nombre al sitio.";
            return;
        }

        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "La contraseña no puede estar vacía.";
            return;
        }

        IsBusy = true;
        try
        {
            var entry = new CredentialEntry
            {
                Id = _id ?? Guid.NewGuid().ToString(),
                SiteName = SiteName.Trim(),
                Category = Category,
                IsFavorite = IsFavorite,
                Username = Username,
                Password = Password,
                Website = Website,
                Notes = Notes,
            };

            await _credentialRepository.SaveAsync(entry);
            Closed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (_id is null)
            return;

        IsBusy = true;
        try
        {
            await _credentialRepository.DeleteAsync(_id);
            Closed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CopyAsync(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        await Clipboard.Default.SetTextAsync(value);
    }
}
