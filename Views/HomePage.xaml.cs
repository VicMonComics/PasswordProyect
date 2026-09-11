using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Common;
using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public HomePage(HomeViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
        _viewModel.OpenCredentialRequested += OnOpenCredentialRequested;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnTabSelected(object? sender, string tab) =>
        await TabNavigationHelper.NavigateToTabAsync(Navigation, _serviceProvider, tab, this);

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<AddEditCredentialPage>();
        page.Initialize(entryId: null);
        await Navigation.PushAsync(page);
    }

    private async void OnOpenCredentialRequested(object? sender, string id)
    {
        var page = _serviceProvider.GetRequiredService<AddEditCredentialPage>();
        page.Initialize(entryId: id);
        await Navigation.PushAsync(page);
    }
}
