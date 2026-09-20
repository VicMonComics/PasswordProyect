namespace PasswordSave.Common;

/// <summary>
/// Claves de Preferences que escriben tanto GooglePlayBillingService como
/// AppleBillingService — así PremiumStatusService.IsPremiumAsync() no
/// necesita saber en qué plataforma corre: ambas dejan el mismo rastro.
/// </summary>
internal static class PremiumPreferenceKeys
{
    public const string IsPremium = "passwordsave.premium.active";

    /// <summary>Solo iOS: respaldo offline (Android confía en RestorePurchases contra Play en cada RefreshAsync).</summary>
    public const string ExpiresAtTicks = "passwordsave.premium.expires";

    /// <summary>Solo Android: quedan guardados por si se necesitan para soporte/depuración.</summary>
    public const string PurchaseToken = "passwordsave.premium.token";
    public const string OrderId = "passwordsave.premium.orderid";
}
