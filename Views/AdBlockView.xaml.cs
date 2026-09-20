#if ANDROID
using Plugin.MauiMTAdmob.Controls;
#endif
using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Common;

public partial class AdBlockView : ContentView
{
    public static readonly BindableProperty AdsIdProperty = BindableProperty.Create(
        nameof(AdsId), typeof(string), typeof(AdBlockView), string.Empty);

#if ANDROID
    private MTAdView? _adView;
#endif

    public AdBlockView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Ad Unit ID de AdMob para este bloque — ver AdUnitIds.</summary>
    public string AdsId
    {
        get => (string)GetValue(AdsIdProperty);
        set => SetValue(AdsIdProperty, value);
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        // ContentView no recibe servicios por constructor (no lo resuelve
        // el contenedor de DI, lo crea el parser de XAML) — se toma
        // prestado el mismo truco que usan los lifecycle events en
        // MauiProgram.cs.
        var services = IPlatformApplication.Current?.Services;
        var premiumStatusService = services?.GetService<IPremiumStatusService>();
        var isPremium = premiumStatusService is not null && await premiumStatusService.IsPremiumAsync();

        if (isPremium)
        {
            IsVisible = false;
            HeightRequest = 0;
            return;
        }

        IsVisible = true;

#if ANDROID
        _adView = new MTAdView
        {
            AdsId = AdsId,
            BackgroundColor = Colors.Transparent,
            HeightRequest = 50,
        };
        Container.Children.Add(_adView);

        EstadoPublicidad.Registrar(_adView);
        EstadoPublicidad.EstadoCambio += ActualizarVisual;
        ActualizarVisual();
#else
        // PENDIENTE: sin AdMob configurado para iOS todavía (tu .csproj de
        // referencia tampoco lo traía) — mientras tanto, iOS siempre
        // muestra el carrusel interno en vez de intentar un anuncio real.
        // El día que agregues el NuGet/config de AdMob para iOS, esta rama
        // se vuelve simétrica a la de Android.
        FallbackCarousel.IsVisible = true;
#endif
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
#if ANDROID
        EstadoPublicidad.EstadoCambio -= ActualizarVisual;
#endif
    }

#if ANDROID
    private void ActualizarVisual()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_adView is not null)
                _adView.IsVisible = !EstadoPublicidad.MostrarPublicidadInterna;

            FallbackCarousel.IsVisible = EstadoPublicidad.MostrarPublicidadInterna;
        });
    }
#endif
}
