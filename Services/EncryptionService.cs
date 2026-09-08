using System;
using System.Security.Cryptography;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Cifra y descifra datos con AES-256-GCM (cifrado autenticado: además de
/// confidencialidad, detecta si el dato fue manipulado). Usa
/// <see cref="System.Security.Cryptography.AesGcm"/>, parte del Base Class
/// Library de .NET — ninguna dependencia externa.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private const int NonceSizeBytes = 12; // tamaño estándar recomendado para GCM
    private const int TagSizeBytes = 16;
    private const int RequiredKeySizeBytes = 32; // AES-256

    public EncryptedPayload Encrypt(byte[] plaintext, byte[] key, byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);

        // El nonce DEBE ser único por cada cifrado hecho con la misma clave.
        // Nunca lo reutilices ni lo derives de forma predecible: reusar un
        // nonce con AES-GCM rompe por completo la confidencialidad del esquema.
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        return new EncryptedPayload
        {
            Nonce = nonce,
            Tag = tag,
            Ciphertext = ciphertext
        };
    }

    public byte[] Decrypt(EncryptedPayload payload, byte[] key, byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ValidateKey(key);

        var plaintext = new byte[payload.Ciphertext.Length];

        using var aesGcm = new AesGcm(key, TagSizeBytes);

        try
        {
            aesGcm.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext, associatedData);
        }
        catch (CryptographicException)
        {
            // El tag de autenticación no coincide: los datos fueron alterados,
            // o la clave/contraseña maestra es incorrecta. No des más detalle
            // que este al llamador (evita filtrar cuál de las dos cosas fue).
            CryptographicOperations.ZeroMemory(plaintext);
            throw new CryptographicException(
                "No fue posible descifrar los datos: clave inválida o datos corruptos.");
        }

        return plaintext;
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != RequiredKeySizeBytes)
            throw new ArgumentException(
                $"La clave debe ser de {RequiredKeySizeBytes * 8} bits ({RequiredKeySizeBytes} bytes) para AES-256.",
                nameof(key));
    }
}
