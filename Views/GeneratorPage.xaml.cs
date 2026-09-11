using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Common;
using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class GeneratorPage : ContentPage
{
    private readonly GeneratorViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public GeneratorPage(GeneratorViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    private async void OnTabSelected(object? sender, string tab) =>
        await TabNavigationHelper.NavigateToTabAsync(Navigation, _serviceProvider, tab, this);

    private async void OnUseClicked(object? sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<AddEditCredentialPage>();
        page.Initialize(entryId: null, prefilledPassword: _viewModel.Password);
        await Navigation.PushAsync(page);
    }
}
