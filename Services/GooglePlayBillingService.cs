#if ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.BillingClient.Api;
using Android.Content;
using Microsoft.Maui.Storage;
using PasswordSave.Common;
using Application = Android.App.Application;

namespace PasswordSave.Services;

/// <summary>
/// Adaptado de tu GooglePlayBillingService ya probado (v8.3 de
/// Xamarin.Android.Google.BillingClient) — misma mecánica de
/// conexión/consulta/compra/acknowledge, solo con los SKUs y las claves de
/// Preferences de PasswordSave.
/// </summary>
public sealed class GooglePlayBillingService : Java.Lang.Object, IPurchasesUpdatedListener
{
    private static GooglePlayBillingService? _instance;
    public static GooglePlayBillingService Instance => _instance ??= new GooglePlayBillingService();

    private readonly Context _context;
    private readonly BillingClient _billingClient;
    private TaskCompletionSource<bool>? _purchaseTcs;

    public GooglePlayBillingService()
    {
        _context = Application.Context;
        _billingClient = BillingClient.NewBuilder(_context)
            .SetListener(this)
            .EnablePendingPurchases(
                PendingPurchasesParams.NewBuilder()
                    .EnableOneTimeProducts()
                    .EnablePrepaidPlans()
                    .Build())
            .Build();
    }

    private async Task EnsureConnectedAsync()
    {
        if (_billingClient.IsReady)
            return;

        var result = await _billingClient.StartConnectionAsync();
        if (result.ResponseCode != BillingResponseCode.Ok)
            throw new Exception($"No se pudo conectar con Google Play: {result.DebugMessage}");
    }

    public async Task<(string Monthly, string Yearly)> GetSubscriptionPricesAsync()
    {
        await EnsureConnectedAsync();
        var details = await QueryProductDetailsInternalAsync(PremiumProductIds.Monthly, PremiumProductIds.Yearly);

        var monthly = details.FirstOrDefault(d => d.ProductId == PremiumProductIds.Monthly);
        var yearly = details.FirstOrDefault(d => d.ProductId == PremiumProductIds.Yearly);

        return (GetFormattedPrice(monthly), GetFormattedPrice(yearly));
    }

    private static string GetFormattedPrice(ProductDetails? productDetails)
    {
        if (productDetails is null)
            return "—";

        try
        {
            var offers = productDetails.GetSubscriptionOfferDetails();
            if (offers is null || offers.Count == 0)
                return "—";

            var phaseList = offers[0].PricingPhases?.PricingPhaseList;
            if (phaseList is null || phaseList.Count == 0)
                return "—";

            return phaseList[0].FormattedPrice ?? "—";
        }
        catch
        {
            return "—";
        }
    }

    public async Task<bool> LaunchPurchaseFlowAsync(string productId)
    {
        await EnsureConnectedAsync();

        var details = await QueryProductDetailsInternalAsync(productId);
        var product = details.FirstOrDefault()
            ?? throw new Exception($"Producto '{productId}' no encontrado en Play Store");

        var offers = product.GetSubscriptionOfferDetails();
        if (offers is null || offers.Count == 0)
            throw new Exception("Sin ofertas disponibles para este producto");

        var offerToken = offers[0].OfferToken;

        _purchaseTcs = new TaskCompletionSource<bool>();

        var productParams = BillingFlowParams.ProductDetailsParams.NewBuilder()
            .SetProductDetails(product)
            .SetOfferToken(offerToken)
            .Build();

        var flowParams = BillingFlowParams.NewBuilder()
            .SetProductDetailsParamsList(new List<BillingFlowParams.ProductDetailsParams> { productParams })
            .Build();

        var activity = Platform.CurrentActivity
            ?? throw new Exception("No hay Activity activa");

        var billingResult = _billingClient.LaunchBillingFlow(activity, flowParams);
        if (billingResult.ResponseCode != BillingResponseCode.Ok)
            throw new Exception($"Error Play Store: {billingResult.DebugMessage}");

        return await _purchaseTcs.Task;
    }

    public async Task<bool> RestorePurchasesAsync()
    {
        await EnsureConnectedAsync();

        var queryParams = QueryPurchasesParams.NewBuilder()
            .SetProductType(BillingClient.ProductType.Subs)
            .Build();

        var result = await _billingClient.QueryPurchasesAsync(queryParams);
        var purchases = result?.Purchases;

        if (purchases is null || purchases.Count == 0)
        {
            Preferences.Default.Set(PremiumPreferenceKeys.IsPremium, false);
            return false;
        }

        foreach (var purchase in purchases)
        {
            if (purchase.PurchaseState == PurchaseState.Purchased)
            {
                AcknowledgePurchase(purchase);
                SavePremium(purchase);
                return true;
            }
        }

        Preferences.Default.Set(PremiumPreferenceKeys.IsPremium, false);
        return false;
    }

    /// <summary>Se llama al iniciar la app (ver App.OnStart): si la suscripción ya no está activa, quita Premium.</summary>
    public async Task RefreshSubscriptionStatusAsync()
    {
        try
        {
            await RestorePurchasesAsync();
        }
        catch
        {
            // Sin conexión con Play: se deja el valor de Preferences como
            // estaba — beneficio de la duda, igual que en tu app original.
        }
    }

    public void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases)
    {
        if (result.ResponseCode == BillingResponseCode.Ok && purchases is not null)
        {
            foreach (var purchase in purchases)
            {
                if (purchase.PurchaseState == PurchaseState.Purchased)
                {
                    AcknowledgePurchase(purchase);
                    SavePremium(purchase);
                    _purchaseTcs?.TrySetResult(true);
                    return;
                }
            }
        }

        _purchaseTcs?.TrySetResult(false);
    }

    private void AcknowledgePurchase(Purchase purchase)
    {
        if (purchase.IsAcknowledged)
            return;

        var param = AcknowledgePurchaseParams.NewBuilder()
            .SetPurchaseToken(purchase.PurchaseToken)
            .Build();

        _ = _billingClient.AcknowledgePurchaseAsync(param);
    }

    private static void SavePremium(Purchase purchase)
    {
        Preferences.Default.Set(PremiumPreferenceKeys.IsPremium, true);
        Preferences.Default.Set(PremiumPreferenceKeys.PurchaseToken, purchase.PurchaseToken);
        Preferences.Default.Set(PremiumPreferenceKeys.OrderId, purchase.OrderId);
    }

    private async Task<List<ProductDetails>> QueryProductDetailsInternalAsync(params string[] skus)
    {
        var productList = skus.Select(sku =>
            QueryProductDetailsParams.Product.NewBuilder()
                .SetProductId(sku)
                .SetProductType(BillingClient.ProductType.Subs)
                .Build()
        ).ToList();

        var param = QueryProductDetailsParams.NewBuilder()
            .SetProductList(productList)
            .Build();

        var result = await _billingClient.QueryProductDetailsAsync(param);
        if (result is null)
            return new List<ProductDetails>();

        return result.ProductDetailsList?.ToList() ?? new List<ProductDetails>();
    }
}
#endif
