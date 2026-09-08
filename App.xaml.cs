using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Services.Abstractions;
using PasswordSave.Views;

namespace PasswordSave;

public partial class App : Application
{
    private readonly IAppLockService _appLockService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IServiceProvider _serviceProvider;

    // StartupPage NO se inyecta como parámetro del constructor: los
    // parámetros del constructor se resuelven (y su XAML se parsea) ANTES
    // de que el cuerpo del constructor corra InitializeComponent(), que es
    // justo lo que fusiona Colors.xaml/Styles.xaml en Application.Resources.
    // Si StartupPage llegara como parámetro, su XAML se parsearía sin esos
    // recursos todavía cargados → "StaticResource not found". Por eso se
    // resuelve manualmente aquí abajo, después de InitializeComponent().
    public App(IAppLockService appLockService, IVaultSessionService vaultSessionService, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _appLockService = appLockService;
        _vaultSessionService = vaultSessionService;
        _serviceProvider = serviceProvider;

        // Cualquier bloqueo (inactividad, manual, o exceso de intentos
        // fallidos) limpia la vault key de memoria y regresa al login.
        _appLockService.Locked += OnAppLocked;

        var startupPage = _serviceProvider.GetRequiredService<StartupPage>();
        MainPage = new NavigationPage(startupPage);
    }

    private void OnAppLocked(object? sender, EventArgs e)
    {
        _vaultSessionService.Clear();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
            MainPage = new NavigationPage(loginPage);
        });
    }
}