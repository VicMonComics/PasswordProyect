using System.Threading.Tasks;

namespace PasswordSave.Services.Abstractions;

public enum PremiumPlan
{
    Monthly,
    Yearly
}

/// <summary>
/// Responde si el usuario tiene la membresía Premium activa y expone la
/// compra/restauración. La implementación real usa Google Play Billing en
/// Android y Plugin.InAppBilling (StoreKit) en iOS — el resto de la app
/// (Store, Categorías, límite de credenciales) solo depende de esta
/// interfaz, nunca de la tienda directamente.
/// </summary>
public interface IPremiumStatusService
{
    Task<bool> IsPremiumAsync();

    /// <summary>Vuelve a validar la suscripción contra la tienda (o el respaldo local sin internet). Llamar al iniciar la app.</summary>
    Task RefreshAsync();

    /// <summary>Precios ya formateados por la tienda (con moneda y símbolo local del usuario), listos para mostrar.</summary>
    Task<(string Monthly, string Yearly)> GetPricesAsync();

    /// <summary>Lanza el flujo de compra nativo para el plan elegido. Devuelve true solo si se completó la compra.</summary>
    Task<bool> PurchaseAsync(PremiumPlan plan);

    /// <summary>Restaura compras ya hechas (requerido por Apple; en Android es más una confirmación manual).</summary>
    Task<bool> RestorePurchasesAsync();
}
