using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using PasswordSave.Services;
using PasswordSave.Services.Abstractions;
using PasswordSave.ViewModels;
using PasswordSave.Views;
using Plugin.Maui.Biometric;
#if ANDROID
using Plugin.MauiMTAdmob;
#endif

namespace PasswordSave;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
#if ANDROID
                .UseMauiMTAdmob()
#endif
            .ConfigureFonts(fonts =>
            {
                // Agrega los .ttf reales a Resources/Fonts/ (descárgalos de
                // Google Fonts: Space Grotesk y Inter) — mientras no estén,
                // estos alias caen al font del sistema como fallback, sin
                // romper el build.
                fonts.AddFont("SpaceGrotesk-Medium.ttf", "SpaceGroteskMedium");
                fonts.AddFont("SpaceGrotesk-SemiBold.ttf", "SpaceGroteskSemibold");
                fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemibold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        RegisterSecurityServices(builder.Services);
        RegisterViewsAndViewModels(builder.Services);
        RegisterLifecycleEvents(builder);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Todos como Singleton: KeyDerivationService y EncryptionService no
    /// tienen estado propio (son puras funciones sobre los bytes que
    /// reciben); VaultKeyService y AppLockService sí manejan estado, pero
    /// ese estado vive en SecureStorage/Preferences, no en memoria — así que
    /// una sola instancia compartida en toda la app es lo correcto (evita
    /// además tener varios contadores de intentos fallidos desincronizados).
    /// </summary>
    private static void RegisterSecurityServices(IServiceCollection services)
    {
        services.AddSingleton<IKeyDerivationService, KeyDerivationService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IVaultKeyService, VaultKeyService>();
        services.AddSingleton<IAppLockService, AppLockService>();
        services.AddSingleton<IVaultSessionService, VaultSessionService>();

        // Plugin.Maui.Biometric: instancia estática del plugin registrada
        // bajo su propia interfaz (IBiometric), y nuestra envoltura encima
        // (IDeviceBiometricService) es lo único que el resto de la app conoce.
        services.AddSingleton(BiometricAuthenticationService.Default);
        services.AddSingleton<IDeviceBiometricService, DeviceBiometricService>();

        // IVaultMetadataStore: metadatos del vault (salt, iteraciones, vault
        // key envuelta) persistidos en SQLite local vía Microsoft.Data.Sqlite
        // (proveedor ADO.NET oficial de Microsoft — no cifra nada por sí
        // mismo, solo guarda los bytes que ya llegan cifrados).
        services.AddSingleton<IVaultMetadataStore, VaultMetadataStore>();

        // IPremiumStatusService: implementación real (Google Play Billing /
        // Plugin.InAppBilling). Antes era StubPremiumStatusService — ese
        // archivo se queda en el proyecto sin usar, por si algún día hace
        // falta para pruebas.
        services.AddSingleton<IPremiumStatusService, PremiumStatusService>();

        services.AddSingleton<IPasswordGeneratorService, PasswordGeneratorService>();

        // IBackupService: empaqueta/restaura el vault completo (backup
        // cifrado exportable) — reusa los bytes ya cifrados, no vuelve a
        // cifrar nada.
        services.AddSingleton<IBackupService, BackupService>();

        // ICredentialRepository: cifra username/password/website/notes por
        // campo con la vault key de la sesión (IVaultSessionService); lanza
        // si el vault está bloqueado.
        services.AddSingleton<ICredentialRepository, CredentialRepository>();
    }

    private static void RegisterViewsAndViewModels(IServiceCollection services)
    {
        services.AddTransient<StartupPage>();
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<OnboardingPage>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginPage>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<HomePage>();
        services.AddTransient<GeneratorViewModel>();
        services.AddTransient<GeneratorPage>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CategoriesPage>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<PaywallPage>();
        services.AddTransient<AddEditCredentialViewModel>();
        services.AddTransient<AddEditCredentialPage>();
        services.AddTransient<StoreViewModel>();
        services.AddTransient<StorePage>();
    }

    /// <summary>
    /// Conecta los eventos de ciclo de vida nativos de cada plataforma con
    /// IAppLockService, para que "ocultar contenido en background" dispare
    /// de inmediato y no dependa de que la UI recuerde llamarlo.
    /// Alternativa más simple (menos precisa en iOS): sobrescribir
    /// OnSleep()/OnResume() en App.xaml.cs.
    /// </summary>
    private static void RegisterLifecycleEvents(MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnPause(_ => GetLockService()?.OnAppBackgrounded())
                .OnResume(_ => GetLockService()?.OnAppForegrounded()));
#elif IOS || MACCATALYST
            events.AddiOS(ios => ios
                // DidEnterBackground/WillEnterForeground (sin prefijo "On",
                // a diferencia de Android) son los nombres reales de la API
                // de Microsoft.Maui.LifecycleEvents para iOS.
                .DidEnterBackground(_ => GetLockService()?.OnAppBackgrounded())
                .WillEnterForeground(_ => GetLockService()?.OnAppForegrounded()));
#endif
        });
    }

    private static IAppLockService? GetLockService() =>
        IPlatformApplication.Current?.Services.GetService<IAppLockService>();
}
