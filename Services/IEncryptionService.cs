using PasswordSave.Models;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Cifra y descifra datos con AES-256-GCM (cifrado autenticado).
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Cifra <paramref name="plaintext"/> con <paramref name="key"/> (debe ser de 32 bytes).
    /// <paramref name="associatedData"/> es opcional: datos que se autentican pero NO se
    /// cifran (por ejemplo, el id de la entrada), útil para evitar que un atacante
    /// reordene o reasigne bloques cifrados entre registros distintos.
    /// </summary>
    EncryptedPayload Encrypt(byte[] plaintext, byte[] key, byte[]? associatedData = null);

    /// <summary>
    /// Descifra un <see cref="EncryptedPayload"/>. Lanza <see cref="System.Security.Cryptography.CryptographicException"/>
    /// si la clave es incorrecta o los datos fueron manipulados.
    /// </summary>
    byte[] Decrypt(EncryptedPayload payload, byte[] key, byte[]? associatedData = null);
}
