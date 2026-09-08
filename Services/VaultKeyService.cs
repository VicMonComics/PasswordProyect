using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Implementación de <see cref="IVaultKeyService"/>. El envoltorio con la
/// master key usa <see cref="IEncryptionService"/> (AES-256-GCM); el
/// desbloqueo rápido usa <see cref="SecureStorage"/> de .NET MAUI, que en
/// Android queda respaldado por el Keystore y en iOS por el Keychain —
/// ambos parte del SDK de MAUI, ninguna dependencia externa.
/// </summary>
public sealed class VaultKeyService : IVaultKeyService
{
    private const string QuickUnlockStorageKey = "passwordsave.vaultkey.quickunlock";
    private const int VaultKeySizeBytes = 32; // 256 bits

    private readonly IEncryptionService _encryptionService;

    public VaultKeyService(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public byte[] GenerateVaultKey() => RandomNumberGenerator.GetBytes(VaultKeySizeBytes);

    public EncryptedPayload WrapWithMasterKey(byte[] vaultKey, byte[] masterKey)
    {
        ValidateVaultKey(vaultKey);

        // Sin associatedData: es un solo secreto autocontenido, no una entrada
        // más entre muchas que necesite atarse a un id externo.
        return _encryptionService.Encrypt(vaultKey, masterKey);
    }

    public byte[] UnwrapWithMasterKey(EncryptedPayload wrappedVaultKey, byte[] masterKey)
    {
        var vaultKey = _encryptionService.Decrypt(wrappedVaultKey, masterKey);
        ValidateVaultKey(vaultKey);
        return vaultKey;
    }

    public async Task StoreForQuickUnlockAsync(byte[] vaultKey)
    {
        ValidateVaultKey(vaultKey);
        var base64 = Convert.ToBase64String(vaultKey);
        await SecureStorage.Default.SetAsync(QuickUnlockStorageKey, base64).ConfigureAwait(false);
    }

    public async Task<byte[]?> TryGetQuickUnlockKeyAsync()
    {
        try
        {
            var base64 = await SecureStorage.Default.GetAsync(QuickUnlockStorageKey).ConfigureAwait(false);
            return base64 is null ? null : Convert.FromBase64String(base64);
        }
        catch
        {
            // En Android, si el usuario cambió el PIN del sistema o se
            // invalidó el Keystore, SecureStorage puede lanzar en vez de
            // devolver null. Se trata igual: no hay desbloqueo rápido
            // disponible, y el flujo debe caer a pedir la contraseña maestra.
            return null;
        }
    }

    public Task ClearQuickUnlockAsync()
    {
        SecureStorage.Default.Remove(QuickUnlockStorageKey);
        return Task.CompletedTask;
    }

    private static void ValidateVaultKey(byte[] vaultKey)
    {
        if (vaultKey is null || vaultKey.Length != VaultKeySizeBytes)
            throw new ArgumentException(
                $"La vault key debe ser de {VaultKeySizeBytes * 8} bits ({VaultKeySizeBytes} bytes).",
                nameof(vaultKey));
    }
}
