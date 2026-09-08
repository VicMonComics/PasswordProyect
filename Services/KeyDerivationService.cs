using System;
using System.Security.Cryptography;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Deriva claves criptográficas a partir de la contraseña maestra usando PBKDF2-SHA256.
/// No depende de librerías externas: usa <see cref="System.Security.Cryptography"/>,
/// incluido en el Base Class Library de .NET (no es un paquete NuGet), con
/// implementaciones auditadas por Microsoft sobre los providers nativos del SO.
/// </summary>
public sealed class KeyDerivationService : IKeyDerivationService
{
    // OWASP recomienda un mínimo de 210,000 iteraciones para PBKDF2-HMAC-SHA256 (2023+).
    // Este valor viaja junto al salt en los parámetros de cada usuario, así que puede
    // subirse en el futuro (nuevos registros) sin romper los vaults ya creados.
    public const int DefaultIterations = 210_000;
    public const int SaltSizeBytes = 16;
    public const int KeyLengthBytes = 32; // 256 bits, para AES-256

    public byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSizeBytes);

    public KdfParameters CreateParameters(byte[]? salt = null, int? iterations = null)
    {
        return new KdfParameters(
            Salt: salt ?? GenerateSalt(),
            Iterations: iterations ?? DefaultIterations,
            KeyLengthBytes: KeyLengthBytes);
    }

    public byte[] DeriveKey(string password, KdfParameters parameters)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("La contraseña maestra no puede estar vacía.", nameof(password));

        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Salt is null || parameters.Salt.Length == 0)
            throw new ArgumentException("El salt es requerido para derivar la clave.", nameof(parameters));

        if (parameters.Iterations < 100_000)
            throw new ArgumentException(
                "Las iteraciones son demasiado bajas para uso seguro (mínimo sugerido: 100,000).",
                nameof(parameters));

        // Rfc2898DeriveBytes.Pbkdf2 es un método estático (.NET 8+), no requiere
        // instanciar ni liberar un objeto IDisposable.
        return Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: parameters.Salt,
            iterations: parameters.Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: parameters.KeyLengthBytes);
    }
}
