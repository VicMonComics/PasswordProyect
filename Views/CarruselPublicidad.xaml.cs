using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Views;

namespace PasswordSave.Common;

public partial class CarruselPublicidad : ContentView
{
    private int _indiceActual;
    private CancellationTokenSource? _token;

    public ObservableCollection<PublicidadSlideItem> Slides { get; } = new()
    {
        new PublicidadSlideItem { Title = "Contraseñas ilimitadas", Subtitle = "El plan Free guarda hasta 15 — Premium no tiene límite" },
        new PublicidadSlideItem { Title = "Carpetas ilimitadas", Subtitle = "Organiza por trabajo, banco, familia y más" },
        new PublicidadSlideItem { Title = "Sin anuncios", Subtitle = "Quita la publicidad de toda la app" },
    };

    public CarruselPublicidad()
    {
        InitializeComponent();
        Carousel.ItemsSource = Slides;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e) => IniciarRotacion();

    private void OnUnloaded(object? sender, EventArgs e) => _token?.Cancel();

    private void IniciarRotacion()
    {
        _token?.Cancel();
        _token = new CancellationTokenSource();
        _ = RotarAsync(_token.Token);
    }

    private async Task RotarAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    break;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Slides.Count == 0)
                        return;

                    _indiceActual = (_indiceActual + 1) % Slides.Count;
                    Carousel.Position = _indiceActual;
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnTapped(object? sender, EventArgs e)
    {
        // ContentView no tiene Navigation propio: se resuelve la página
        // actual desde la ventana de la app, igual que en App.xaml.cs.
        var navigation = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page?.Navigation : null;
        var services = IPlatformApplication.Current?.Services;

        if (navigation is null || services is null)
            return;

        var storePage = services.GetRequiredService<StorePage>();
        await navigation.PushModalAsync(storePage);
    }
}
