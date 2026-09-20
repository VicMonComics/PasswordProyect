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
        _viewModel.RestoredFromBackup += OnRestoredFromBackup;
    }

    private async void OnVaultCreated(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<HomePage>());
        Navigation.RemovePage(this);
    }

    private async void OnRestoredFromBackup(object? sender, EventArgs e)
    {
        // A diferencia de crear un vault nuevo, restaurar un backup NO deja
        // la sesión desbloqueada — el usuario tiene que iniciar sesión con
        // su contraseña maestra de siempre, como en cualquier login normal.
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<LoginPage>());
        Navigation.RemovePage(this);
    }
}
