using Microsoft.Extensions.DependencyInjection;
using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public LoginPage(LoginViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
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
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<HomePage>());
        Navigation.RemovePage(this);
    }
}
