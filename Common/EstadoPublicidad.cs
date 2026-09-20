#if ANDROID
using Plugin.MauiMTAdmob.Controls;

namespace PasswordSave.Common;

/// <summary>
/// Adaptado de tu EstadoPublicidad. Solo Android porque Plugin.MauiMTAdmob
/// (MTAdView) solo está agregado como NuGet para ese target — ver el
/// comentario en AdBlockView sobre iOS.
/// </summary>
public static class EstadoPublicidad
{
    public static bool MostrarPublicidadInterna { get; private set; }

    public static event Action? EstadoCambio;

    private static int _totalBanners;
    private static int _bannersRespondidos;
    private static int _bannersCorrectos;

    public static void Registrar(params MTAdView[] banners)
    {
        Reiniciar();
        _totalBanners = banners.Length;

        foreach (var banner in banners)
        {
            banner.AdsLoaded += Banner_AdsLoaded;
            banner.AdsFailedToLoad += Banner_AdsFailedToLoad;
        }
    }

    private static void Banner_AdsLoaded(object? sender, EventArgs e)
    {
        _bannersRespondidos++;
        _bannersCorrectos++;
        EvaluarEstado();
    }

    // dynamic en el segundo parámetro: igual que en tu código original —
    // no depende de conocer el tipo exacto del EventArgs de fallo del
    // plugin, se resuelve en tiempo de ejecución.
    private static void Banner_AdsFailedToLoad(object? sender, dynamic e)
    {
        _bannersRespondidos++;
        EvaluarEstado();
    }

    private static void EvaluarEstado()
    {
        if (_bannersRespondidos < _totalBanners)
            return;

        MostrarPublicidadInterna = _bannersCorrectos == 0;
        EstadoCambio?.Invoke();
    }

    public static void Reiniciar()
    {
        MostrarPublicidadInterna = false;
        _totalBanners = 0;
        _bannersRespondidos = 0;
        _bannersCorrectos = 0;
    }
}
#endif
