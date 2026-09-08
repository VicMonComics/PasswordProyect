using System.Security.Cryptography;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Implementación en memoria de <see cref="IVaultSessionService"/>. Debe
/// registrarse como Singleton: es, junto con AppLockService, el estado
/// central de "la app está desbloqueada ahora mismo".
/// </summary>
public sealed class VaultSessionService : IVaultSessionService
{
    private byte[]? _vaultKey;

    public bool IsUnlocked => _vaultKey is not null;

    public byte[]? CurrentVaultKey => _vaultKey;

    public void SetUnlocked(byte[] vaultKey)
    {
        // Por si había una key previa cargada, se borra antes de reemplazarla.
        Clear();
        _vaultKey = vaultKey;
    }

    public void Clear()
    {
        if (_vaultKey is not null)
        {
            CryptographicOperations.ZeroMemory(_vaultKey);
            _vaultKey = null;
        }
    }
}
