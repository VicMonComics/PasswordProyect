using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.VaultCreated += OnVaultCreated;
    }

    private async void OnVaultCreated(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new VaultPage());
        Navigation.RemovePage(this);
    }
}
