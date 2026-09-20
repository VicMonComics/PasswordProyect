#if IOS
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using PasswordSave.Common;
using Plugin.InAppBilling;

namespace PasswordSave.Services;

/// <summary>
/// Adaptado de tu PremiumService + ObtenerPrecioMobile ya probados
/// (Plugin.InAppBilling). Misma mecánica: valida online contra StoreKit y
/// cae a un respaldo offline (fecha de expiración calculada y guardada
/// localmente) si no hay internet.
/// </summary>
public static class AppleBillingService
{
    public static async Task<(string Monthly, string Yearly)> GetPricesAsync()
    {
        var billing = CrossInAppBilling.Current;

        try
        {
            var connected = await billing.ConnectAsync();
            if (!connected)
                return ("No disponible", "No disponible");

            var items = await billing.GetProductInfoAsync(
                ItemType.Subscription,
                new[] { PremiumProductIds.Monthly, PremiumProductIds.Yearly });

            var monthly = items?.FirstOrDefault(i => i.ProductId == PremiumProductIds.Monthly)?.LocalizedPrice ?? "No disponible";
            var yearly = items?.FirstOrDefault(i => i.ProductId == PremiumProductIds.Yearly)?.LocalizedPrice ?? "No disponible";

            return (monthly, yearly);
        }
        finally
        {
            await billing.DisconnectAsync();
        }
    }

    public static async Task<bool> PurchaseAsync(string productId)
    {
        var billing = CrossInAppBilling.Current;

        try
        {
            var connected = await billing.ConnectAsync();
            if (!connected)
                return false;

            var purchase = await billing.PurchaseAsync(productId, ItemType.Subscription);
            if (purchase is null)
                return false;

            GuardarLicenciaLocal(productId, purchase.TransactionDateUtc);
            return true;
        }
        finally
        {
            await billing.DisconnectAsync();
        }
    }

    public static async Task<bool> RestorePurchasesAsync()
    {
        var billing = CrossInAppBilling.Current;

        try
        {
            await billing.ConnectAsync();

            var purchases = await billing.GetPurchasesAsync(ItemType.Subscription);
            if (purchases is null)
                return false;

            foreach (var compra in purchases)
            {
                if (compra.ProductId == PremiumProductIds.Monthly || compra.ProductId == PremiumProductIds.Yearly)
                {
                    GuardarLicenciaLocal(compra.ProductId, compra.TransactionDateUtc);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            await billing.DisconnectAsync();
        }
    }

    /// <summary>Se llama al iniciar la app (ver App.OnStart): valida online y, sin internet, cae al respaldo offline.</summary>
    public static async Task RefreshSubscriptionStatusAsync()
    {
        try
        {
            var conectado = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            if (conectado)
            {
                var billing = CrossInAppBilling.Current;
                var connected = await billing.ConnectAsync();

                if (connected)
                {
                    try
                    {
                        var purchases = await billing.GetPurchasesAsync(ItemType.Subscription);
                        if (purchases is not null)
                        {
                            foreach (var compra in purchases)
                            {
                                if (compra.ProductId == PremiumProductIds.Monthly || compra.ProductId == PremiumProductIds.Yearly)
                                {
                                    GuardarLicenciaLocal(compra.ProductId, compra.TransactionDateUtc);
                                    return;
                                }
                            }
                        }
                        // No se encontró suscripción activa en la tienda: no
                        // se quita Premium a ciegas — cae al respaldo
                        // offline, que sí sabe si de verdad ya expiró.
                    }
                    finally
                    {
                        await billing.DisconnectAsync();
                    }
                }
            }

            ValidarLicenciaOffline();
        }
        catch
        {
            ValidarLicenciaOffline();
        }
    }

    private static void GuardarLicenciaLocal(string productId, DateTime fechaCompraUtc)
    {
        var fechaExpiracion = productId == PremiumProductIds.Monthly
            ? fechaCompraUtc.AddMonths(1)
            : fechaCompraUtc.AddYears(1);

        Preferences.Default.Set(PremiumPreferenceKeys.ExpiresAtTicks, fechaExpiracion.Ticks);
        Preferences.Default.Set(PremiumPreferenceKeys.IsPremium, true);
    }

    private static void ValidarLicenciaOffline()
    {
        var esPremium = Preferences.Default.Get(PremiumPreferenceKeys.IsPremium, false);
        if (!esPremium)
            return;

        var ticks = Preferences.Default.Get(PremiumPreferenceKeys.ExpiresAtTicks, 0L);
        if (ticks == 0)
        {
            Preferences.Default.Set(PremiumPreferenceKeys.IsPremium, false);
            return;
        }

        var fechaExpira = new DateTime(ticks, DateTimeKind.Utc);
        if (DateTime.UtcNow > fechaExpira)
            Preferences.Default.Set(PremiumPreferenceKeys.IsPremium, false);
    }
}
#endif
