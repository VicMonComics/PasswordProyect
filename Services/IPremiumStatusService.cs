using System.Threading.Tasks;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Responde si el usuario tiene la membresía Premium activa. Hoy es un stub
/// en memoria — la implementación real se conecta a Google Play Billing /
/// Apple StoreKit (típicamente vía un plugin como Plugin.InAppBilling, un
/// NuGet de binding nativo, en la misma categoría que el de biometría).
/// El resto de la app (categorías, paywall, límite de contraseñas) depende
/// de esta interfaz, no de la tienda directamente — así el día que
/// conectemos la compra real, solo se reemplaza esta implementación.
/// </summary>
public interface IPremiumStatusService
{
    Task<bool> IsPremiumAsync();
}
