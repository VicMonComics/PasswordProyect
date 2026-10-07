using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordSave.Common;

/// <summary>
/// Carrusel de respaldo cuando el anuncio de AdMob no carga (ver
/// AdBlockView). Adaptado de CarruselPublicidad de otros proyectos del
/// cliente: intenta descargar 5 imágenes PNG de un servidor propio, y si
/// no hay internet o la descarga falla, cae a 3 imágenes locales
/// empacadas en el proyecto.
/// </summary>
public partial class CarruselPublicidad : ContentView
{
    private int _indiceActual;
    private CancellationTokenSource? _token;
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    // TODO: URL de prueba (servidor de la app de cuentos del cliente) —
    // reemplazar por el dominio/ruta real de PasswordSave cuando esté
    // listo (p. ej. algo bajo venomsystem.com.mx/app/llavero).
    private const string WebApi = "https://cuentos.venomsystem.com.mx";

    public ObservableCollection<PublicidadItem> Publicidades { get; } = new();

    public CarruselPublicidad()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        _ = CargarPublicidadesAsync();
    }

    private static bool TieneInternet() =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    private async Task CargarPublicidadesAsync()
    {
        try
        {
            Publicidades.Clear();

            if (TieneInternet())
            {
                string[] urls =
                {
                    $"{WebApi}/publicidad/banner00.png",
                    $"{WebApi}/publicidad/banner01.png",
                    $"{WebApi}/publicidad/banner02.png",
                    $"{WebApi}/publicidad/banner03.png",
                    $"{WebApi}/publicidad/banner04.png",
                };

                foreach (var url in urls)
                {
                    try
                    {
                        var response = await HttpClient.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                            continue;

                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        if (bytes.Length == 0 || !EsPng(bytes))
                            continue;

                        Publicidades.Add(new PublicidadItem
                        {
                            Imagen = ImageSource.FromStream(() => new MemoryStream(bytes)),
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error cargando publicidad {url}: {ex.Message}");
                    }
                }
            }

            // Sin internet, o ninguna imagen remota cargó: caer a las 3
            // imágenes locales ya empacadas en el proyecto
            // (Resources/Images/bn1.png, bn2.png, bn3.png).
            if (Publicidades.Count == 0)
            {
                Publicidades.Add(new PublicidadItem { Imagen = "bn1.png" });
                Publicidades.Add(new PublicidadItem { Imagen = "bn2.png" });
                Publicidades.Add(new PublicidadItem { Imagen = "bn3.png" });
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Carousel.ItemsSource = Publicidades;
                _indiceActual = 0;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error general cargando publicidades: {ex}");
        }
    }

    private static bool EsPng(byte[] bytes)
    {
        if (bytes.Length < 8)
            return false;

        return bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
               bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
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
                    if (Publicidades.Count == 0)
                        return;

                    _indiceActual = (_indiceActual + 1) % Publicidades.Count;
                    Carousel.Position = _indiceActual;
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
