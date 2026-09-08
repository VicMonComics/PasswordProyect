using PasswordSave.Models;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Deriva claves criptográficas a partir de la contraseña maestra del usuario.
/// </summary>
public interface IKeyDerivationService
{
    /// <summary>Genera un salt aleatorio criptográficamente seguro.</summary>
    byte[] GenerateSalt();

    /// <summary>
    /// Crea un nuevo conjunto de parámetros KDF. Si no se especifica salt o
    /// iteraciones, usa los valores por defecto recomendados.
    /// </summary>
    KdfParameters CreateParameters(byte[]? salt = null, int? iterations = null);

    /// <summary>
    /// Deriva una clave de 256 bits a partir de la contraseña maestra y los
    /// parámetros KDF (salt + iteraciones). Determinístico: la misma
    /// contraseña + los mismos parámetros siempre producen la misma clave.
    /// </summary>
    byte[] DeriveKey(string password, KdfParameters parameters);
}
