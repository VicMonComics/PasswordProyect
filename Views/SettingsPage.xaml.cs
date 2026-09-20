using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
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
        _viewModel.BackupFileReady += OnBackupFileReady;

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

    private async void OnBackupFileReady(object? sender, string filePath)
    {
        // Share.RequestAsync es del propio SDK de MAUI (Microsoft.Maui.ApplicationModel.DataTransfer,
        // igual que Clipboard) — abre el share sheet nativo del sistema, el
        // usuario decide a dónde mandarlo (WhatsApp, correo, guardar en
        // archivos, etc.), como quedamos desde el principio.
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Backup de PasswordSave",
            File = new ShareFile(filePath),
        });
    }
}
