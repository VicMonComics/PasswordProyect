using System;
using System.Security.Cryptography;
using System.Text;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Genera contraseñas aleatorias criptográficamente seguras.
/// Usa RandomNumberGenerator.GetInt32 (System.Security.Cryptography, BCL —
/// sin NuGets) en vez de Random: además de ser criptográficamente seguro,
/// GetInt32 evita el sesgo de módulo (module bias) que tendría hacer
/// `random.Next() % pool.Length` a mano.
///
/// Los charsets excluyen caracteres ambiguos (I, O, l, 0, 1) para que la
/// contraseña se pueda transcribir a mano sin confusiones — igual que el
/// prototipo de diseño.
/// </summary>
public sealed class PasswordGeneratorService : IPasswordGeneratorService
{
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string NumberChars = "23456789";
    private const string SymbolChars = "!@#$%&*?";

    public string Generate(PasswordGenerationOptions options)
    {
        var pool = BuildPool(options);
        if (pool.Length == 0)
            throw new ArgumentException("Selecciona al menos un tipo de carácter (mayúsculas, minúsculas, números o símbolos).", nameof(options));

        if (options.Length < 6 || options.Length > 64)
            throw new ArgumentException("La longitud debe estar entre 6 y 64 caracteres.", nameof(options));

        var result = new StringBuilder(options.Length);
        for (var i = 0; i < options.Length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(0, pool.Length);
            result.Append(pool[index]);
        }

        return result.ToString();
    }

    public PasswordStrength EstimateStrength(PasswordGenerationOptions options)
    {
        var poolSize = BuildPool(options).Length;
        if (poolSize == 0 || options.Length <= 0)
            return PasswordStrength.Debil;

        // Entropía aproximada en bits: longitud * log2(tamaño del alfabeto).
        var entropyBits = options.Length * Math.Log2(poolSize);

        return entropyBits switch
        {
            < 40 => PasswordStrength.Debil,
            < 60 => PasswordStrength.Aceptable,
            < 80 => PasswordStrength.Segura,
            _ => PasswordStrength.MuySegura
        };
    }

    private static string BuildPool(PasswordGenerationOptions options)
    {
        var pool = new StringBuilder();
        if (options.UseUppercase) pool.Append(UpperChars);
        if (options.UseLowercase) pool.Append(LowerChars);
        if (options.UseNumbers) pool.Append(NumberChars);
        if (options.UseSymbols) pool.Append(SymbolChars);
        return pool.ToString();
    }
}
