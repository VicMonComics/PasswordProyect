using System.Threading.Tasks;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// PENDIENTE DE REEMPLAZAR: siempre "no premium", con precios de ejemplo y
/// sin conectar a ninguna tienda real. Permite construir y probar la
/// pantalla Store con datos plausibles mientras se conecta Google Play
/// Billing / Plugin.InAppBilling detrás de esta misma interfaz.
/// </summary>
public sealed class StubPremiumStatusService : IPremiumStatusService
{
    public Task<bool> IsPremiumAsync() => Task.FromResult(false);

    public Task RefreshAsync() => Task.CompletedTask;

    public Task<(string Monthly, string Yearly)> GetPricesAsync() =>
        Task.FromResult(("$49.00 MXN", "$399.00 MXN"));

    public Task<bool> PurchaseAsync(PremiumPlan plan) => Task.FromResult(false);

    public Task<bool> RestorePurchasesAsync() => Task.FromResult(false);
}
