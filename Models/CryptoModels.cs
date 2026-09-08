using System;

namespace PasswordSave.Models;

/// <summary>
/// Parámetros usados para derivar una clave a partir de una contraseña (PBKDF2).
/// Se guardan junto al dato cifrado (el salt y las iteraciones NO son secretos:
/// sin la contraseña no sirven de nada para un atacante) para poder re-derivar
/// la misma clave más adelante, y para poder subir las iteraciones en el futuro
/// sin romper compatibilidad con vaults ya creados.
/// </summary>
public sealed record KdfParameters(
    byte[] Salt,
    int Iterations,
    int KeyLengthBytes = 32,
    string Algorithm = "PBKDF2-SHA256");

/// <summary>
/// Resultado de un cifrado AES-256-GCM: nonce (único por cifrado), tag de
/// autenticación (detecta manipulación) y el texto cifrado.
/// </summary>
public sealed class EncryptedPayload
{
    private const byte FormatVersion = 1;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public required byte[] Nonce { get; init; }
    public required byte[] Tag { get; init; }
    public required byte[] Ciphertext { get; init; }

    /// <summary>
    /// Serializa a un solo arreglo de bytes listo para guardar en una columna
    /// BLOB de SQLite: [1 byte versión][12 bytes nonce][16 bytes tag][N bytes ciphertext].
    /// </summary>
    public byte[] ToBytes()
    {
        var result = new byte[1 + Nonce.Length + Tag.Length + Ciphertext.Length];
        result[0] = FormatVersion;
        Buffer.BlockCopy(Nonce, 0, result, 1, Nonce.Length);
        Buffer.BlockCopy(Tag, 0, result, 1 + Nonce.Length, Tag.Length);
        Buffer.BlockCopy(Ciphertext, 0, result, 1 + Nonce.Length + Tag.Length, Ciphertext.Length);
        return result;
    }

    public static EncryptedPayload FromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 1 + NonceSizeBytes + TagSizeBytes)
            throw new ArgumentException("Datos cifrados corruptos o incompletos.", nameof(data));

        var version = data[0];
        if (version != FormatVersion)
            throw new NotSupportedException($"Versión de formato de cifrado no soportada: {version}");

        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[data.Length - 1 - NonceSizeBytes - TagSizeBytes];

        Buffer.BlockCopy(data, 1, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(data, 1 + NonceSizeBytes, tag, 0, TagSizeBytes);
        Buffer.BlockCopy(data, 1 + NonceSizeBytes + TagSizeBytes, ciphertext, 0, ciphertext.Length);

        return new EncryptedPayload { Nonce = nonce, Tag = tag, Ciphertext = ciphertext };
    }
}
