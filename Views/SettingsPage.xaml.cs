using Microsoft.Maui.ApplicationModel;
using PasswordSave.Common;
using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public SettingsPage(SettingsViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;

        // VersionTracking es parte del BCL de .NET MAUI (Microsoft.Maui.ApplicationModel) —
        // ningún NuGet extra. Lee la versión real del .csproj (ApplicationDisplayVersion),
        // no un número escrito a mano que se nos pueda olvidar actualizar.
        version.Text = "Versión: " + VersionTracking.Default.CurrentVersion;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnTabSelected(object? sender, string tab) =>
        await TabNavigationHelper.NavigateToTabAsync(Navigation, _serviceProvider, tab, this);
}
