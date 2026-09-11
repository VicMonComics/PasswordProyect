using Microsoft.Extensions.DependencyInjection;
using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public OnboardingPage(OnboardingViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
        _viewModel.VaultCreated += OnVaultCreated;
    }

    private async void OnVaultCreated(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<HomePage>());
        Navigation.RemovePage(this);
    }
}
