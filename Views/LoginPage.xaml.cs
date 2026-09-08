using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnLoginSucceeded(object? sender, EventArgs e)
    {
        // VaultPage es un placeholder temporal: la pantalla real de la
        // bóveda (lista de contraseñas) todavía no la hemos construido.
        await Navigation.PushAsync(new VaultPage());
        Navigation.RemovePage(this);
    }
}
