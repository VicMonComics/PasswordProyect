using System.Threading.Tasks;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// PENDIENTE DE REEMPLAZAR: siempre devuelve false (usuario free) hasta que
/// conectemos la compra real de la tienda (Google Play / App Store). Vive
/// como servicio separado para que ese reemplazo no toque las pantallas que
/// ya consultan IPremiumStatusService.
/// </summary>
public sealed class StubPremiumStatusService : IPremiumStatusService
{
    public Task<bool> IsPremiumAsync() => Task.FromResult(false);
}
