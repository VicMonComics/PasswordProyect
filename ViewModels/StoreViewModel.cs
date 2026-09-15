using System;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordSave.Common;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.ViewModels;

public sealed class StoreViewModel : ObservableObject
{
    private readonly IPremiumStatusService _premiumStatusService;

    private string _monthlyPrice = "—";
    private string _yearlyPrice = "—";
    private PremiumPlan _selectedPlan = PremiumPlan.Yearly; // el anual suele ser el mejor trato: preseleccionado
    private bool _isPremium;
    private bool _isBusy;
    private string? _statusMessage;
    private bool _statusMessageIsError;

    public event EventHandler? PurchaseCompleted;

    public StoreViewModel(IPremiumStatusService premiumStatusService)
    {
        _premiumStatusService = premiumStatusService;

        SelectMonthlyCommand = new RelayCommand(() => SelectedPlan = PremiumPlan.Monthly);
        SelectYearlyCommand = new RelayCommand(() => SelectedPlan = PremiumPlan.Yearly);
        PurchaseCommand = new AsyncRelayCommand(PurchaseAsync, () => !IsBusy && !IsPremium);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync, () => !IsBusy);
    }

    public string MonthlyPrice
    {
        get => _monthlyPrice;
        private set => SetProperty(ref _monthlyPrice, value);
    }

    public string YearlyPrice
    {
        get => _yearlyPrice;
        private set => SetProperty(ref _yearlyPrice, value);
    }

    public PremiumPlan SelectedPlan
    {
        get => _selectedPlan;
        private set
        {
            if (SetProperty(ref _selectedPlan, value))
            {
                OnPropertyChanged(nameof(IsMonthlySelected));
                OnPropertyChanged(nameof(IsYearlySelected));
            }
        }
    }

    // Bools planos en vez de comparar el enum directo en XAML — así no
    // hace falta un converter para "SelectedPlan == Monthly".
    public bool IsMonthlySelected => SelectedPlan == PremiumPlan.Monthly;
    public bool IsYearlySelected => SelectedPlan == PremiumPlan.Yearly;

    public bool IsPremium
    {
        get => _isPremium;
        private set
        {
            if (SetProperty(ref _isPremium, value))
                ((AsyncRelayCommand)PurchaseCommand).RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((AsyncRelayCommand)PurchaseCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)RestoreCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool StatusMessageIsError
    {
        get => _statusMessageIsError;
        private set => SetProperty(ref _statusMessageIsError, value);
    }

    public ICommand SelectMonthlyCommand { get; }
    public ICommand SelectYearlyCommand { get; }
    public ICommand PurchaseCommand { get; }
    public ICommand RestoreCommand { get; }

    public async Task InitializeAsync()
    {
        StatusMessage = null;
        IsPremium = await _premiumStatusService.IsPremiumAsync();

        var (monthly, yearly) = await _premiumStatusService.GetPricesAsync();
        MonthlyPrice = monthly;
        YearlyPrice = yearly;
    }

    private async Task PurchaseAsync()
    {
        StatusMessage = null;
        IsBusy = true;
        try
        {
            var success = await _premiumStatusService.PurchaseAsync(SelectedPlan);
            if (success)
            {
                IsPremium = true;
                SetStatus("¡Ya eres PasswordSave Premium!", isError: false);
                PurchaseCompleted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SetStatus("No se completó la compra. Intenta de nuevo.", isError: true);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Ocurrió un problema: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreAsync()
    {
        StatusMessage = null;
        IsBusy = true;
        try
        {
            var restored = await _premiumStatusService.RestorePurchasesAsync();
            if (restored)
            {
                IsPremium = true;
                SetStatus("Tu compra fue restaurada correctamente.", isError: false);
                PurchaseCompleted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SetStatus("No encontramos ninguna compra previa para restaurar.", isError: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Ocurrió un problema al restaurar: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessageIsError = isError;
        StatusMessage = message;
    }
}
