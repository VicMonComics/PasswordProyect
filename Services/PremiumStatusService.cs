using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using PasswordSave.Common;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Implementación real de IPremiumStatusService: Google Play Billing en
/// Android, Plugin.InAppBilling (StoreKit) en iOS. IsPremiumAsync no
/// necesita ramas #if: ambas plataformas escriben "premium activo" bajo la
/// misma clave de Preferences (PremiumPreferenceKeys.IsPremium).
/// </summary>
public sealed class PremiumStatusService : IPremiumStatusService
{
    public Task<bool> IsPremiumAsync() =>
        Task.FromResult(Preferences.Default.Get(PremiumPreferenceKeys.IsPremium, false));

    public async Task RefreshAsync()
    {
#if ANDROID
        await GooglePlayBillingService.Instance.RefreshSubscriptionStatusAsync();
#elif IOS
        await AppleBillingService.RefreshSubscriptionStatusAsync();
#else
        await Task.CompletedTask;
#endif
    }

    public async Task<(string Monthly, string Yearly)> GetPricesAsync()
    {
#if ANDROID
        return await GooglePlayBillingService.Instance.GetSubscriptionPricesAsync();
#elif IOS
        return await AppleBillingService.GetPricesAsync();
#else
        return ("No disponible", "No disponible");
#endif
    }

    public async Task<bool> PurchaseAsync(PremiumPlan plan)
    {
        var productId = plan == PremiumPlan.Monthly ? PremiumProductIds.Monthly : PremiumProductIds.Yearly;

#if ANDROID
        return await GooglePlayBillingService.Instance.LaunchPurchaseFlowAsync(productId);
#elif IOS
        return await AppleBillingService.PurchaseAsync(productId);
#else
        return false;
#endif
    }

    public async Task<bool> RestorePurchasesAsync()
    {
#if ANDROID
        return await GooglePlayBillingService.Instance.RestorePurchasesAsync();
#elif IOS
        return await AppleBillingService.RestorePurchasesAsync();
#else
        return false;
#endif
    }
}
