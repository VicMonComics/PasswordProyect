using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Services.Abstractions;
using PasswordSave.Views;

namespace PasswordSave;

public partial class App : Application
{
    private readonly IAppLockService _appLockService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IServiceProvider _serviceProvider;

    private bool _isPrivacyOverlayShown;

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

        // Cubre/descubre el contenido al pasar a segundo plano — se dispara
        // de inmediato (ver AppLockService.OnAppBackgrounded), sin esperar
        // el timeout de inactividad, para proteger la miniatura del
        // selector de apps recientes.
        _appLockService.ContentVisibilityChanged += OnContentVisibilityChanged;

        var startupPage = _serviceProvider.GetRequiredService<StartupPage>();
        MainPage = new NavigationPage(startupPage);
    }

    protected override void OnStart()
    {
        base.OnStart();

        // Revalida la suscripción contra la tienda (o el respaldo offline
        // si no hay internet) cada vez que la app arranca — igual que
        // VerificarSuscripcionAndroid/EvaluarLicenciaAsync en tu otro
        // proyecto. Fire-and-forget: no debe bloquear el arranque de la UI.
        var premiumStatusService = _serviceProvider.GetRequiredService<IPremiumStatusService>();
        _ = premiumStatusService.RefreshAsync();
    }

    private void OnAppLocked(object? sender, EventArgs e)
    {
        _vaultSessionService.Clear();

        // Al reemplazar MainPage por completo se descarta también cualquier
        // modal que estuviera encima (incluida la cortina de privacidad), así
        // que basta con resetear la bandera — no hace falta hacer PopModalAsync.
        _isPrivacyOverlayShown = false;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
            MainPage = new NavigationPage(loginPage);
        });
    }

    private void OnContentVisibilityChanged(object? sender, bool shouldHide)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var navigation = MainPage?.Navigation;
            if (navigation is null)
                return;

            if (shouldHide && !_isPrivacyOverlayShown)
            {
                _isPrivacyOverlayShown = true;
                // animated: false — tiene que cubrir la pantalla YA, no
                // después de una animación, porque el SO puede tomar la
                // miniatura del multitarea en cualquier momento del tránsito.
                await navigation.PushModalAsync(new PrivacyOverlayPage(), animated: false);
            }
            else if (!shouldHide && _isPrivacyOverlayShown)
            {
                _isPrivacyOverlayShown = false;
                await navigation.PopModalAsync(animated: false);
            }
        });
    }
}
