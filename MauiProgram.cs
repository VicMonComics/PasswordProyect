using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Maui.Biometric;
using PasswordSave.Services;
using PasswordSave.Services.Abstractions;
using PasswordSave.ViewModels;
using PasswordSave.Views;

namespace PasswordSave;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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
    }

    private static void RegisterViewsAndViewModels(IServiceCollection services)
    {
        services.AddTransient<StartupPage>();
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<OnboardingPage>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginPage>();
        services.AddTransient<VaultPage>();
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
