using System.Windows.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using PasswordSave.Common;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.ViewModels;

public sealed class GeneratorViewModel : ObservableObject
{
    private readonly IPasswordGeneratorService _generatorService;

    private string _password = string.Empty;
    private int _length = 14;
    private bool _useUppercase = true;
    private bool _useLowercase = true;
    private bool _useNumbers = true;
    private bool _useSymbols = true;
    private string _strengthLabel = string.Empty;
    private double _strengthFraction;
    private string? _errorMessage;
    private string? _copiedMessage;

    public GeneratorViewModel(IPasswordGeneratorService generatorService)
    {
        _generatorService = generatorService;

        RegenerateCommand = new RelayCommand(Regenerate);
        CopyCommand = new AsyncRelayCommand(CopyAsync, () => !string.IsNullOrEmpty(Password));

        Regenerate();
    }

    public string Password
    {
        get => _password;
        private set
        {
            if (SetProperty(ref _password, value))
                ((AsyncRelayCommand)CopyCommand).RaiseCanExecuteChanged();
        }
    }

    public int Length
    {
        get => _length;
        set
        {
            if (SetProperty(ref _length, value))
                Regenerate();
        }
    }

    public bool UseUppercase
    {
        get => _useUppercase;
        set
        {
            if (SetProperty(ref _useUppercase, value))
                Regenerate();
        }
    }

    public bool UseLowercase
    {
        get => _useLowercase;
        set
        {
            if (SetProperty(ref _useLowercase, value))
                Regenerate();
        }
    }

    public bool UseNumbers
    {
        get => _useNumbers;
        set
        {
            if (SetProperty(ref _useNumbers, value))
                Regenerate();
        }
    }

    public bool UseSymbols
    {
        get => _useSymbols;
        set
        {
            if (SetProperty(ref _useSymbols, value))
                Regenerate();
        }
    }

    public string StrengthLabel
    {
        get => _strengthLabel;
        private set => SetProperty(ref _strengthLabel, value);
    }

    /// <summary>0.0 a 1.0, para el ancho de la barra de fortaleza.</summary>
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

    public string? CopiedMessage
    {
        get => _copiedMessage;
        private set => SetProperty(ref _copiedMessage, value);
    }

    public ICommand RegenerateCommand { get; }
    public ICommand CopyCommand { get; }

    private void Regenerate()
    {
        ErrorMessage = null;
        CopiedMessage = null;

        var options = new PasswordGenerationOptions(Length, UseUppercase, UseLowercase, UseNumbers, UseSymbols);

        try
        {
            Password = _generatorService.Generate(options);
            UpdateStrength(options);
        }
        catch (ArgumentException ex)
        {
            // p. ej. el usuario apagó los 4 switches: no hay charset posible.
            Password = string.Empty;
            ErrorMessage = ex.Message;
            StrengthLabel = string.Empty;
            StrengthFraction = 0;
        }
    }

    private void UpdateStrength(PasswordGenerationOptions options)
    {
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

    private async Task CopyAsync()
    {
        if (string.IsNullOrEmpty(Password))
            return;

        await Clipboard.Default.SetTextAsync(Password);
        CopiedMessage = "Copiada al portapapeles";
    }
}
