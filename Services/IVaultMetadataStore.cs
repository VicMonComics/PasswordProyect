using System.Threading.Tasks;
using PasswordSave.Models;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Lee y escribe los metadatos del vault (parámetros KDF y la vault key
/// envuelta con la contraseña maestra) en la base SQLite local.
/// </summary>
public interface IVaultMetadataStore
{
    /// <summary>True si ya existe un vault creado en este dispositivo.</summary>
    Task<bool> VaultExistsAsync();

    /// <summary>Guarda el vault por primera vez (onboarding). Falla si ya existe uno — usa UpdateVaultAsync para actualizarlo.</summary>
    Task SaveVaultAsync(KdfParameters kdfParameters, EncryptedPayload wrappedVaultKey);

    /// <summary>
    /// Reemplaza los parámetros KDF y la vault key envuelta de un vault que
    /// YA existe (cambio de contraseña maestra: la vault key en sí no
    /// cambia, solo cómo está envuelta — por eso no hace falta re-cifrar
    /// ninguna credencial guardada). Falla si todavía no existe un vault.
    /// </summary>
    Task UpdateVaultAsync(KdfParameters kdfParameters, EncryptedPayload wrappedVaultKey);

    /// <summary>Parámetros KDF (salt + iteraciones) guardados al crear el vault.</summary>
    Task<KdfParameters> GetKdfParametersAsync();

    /// <summary>La vault key, ya envuelta (cifrada) con la master key derivada de la contraseña maestra.</summary>
    Task<EncryptedPayload> GetWrappedVaultKeyAsync();
}
