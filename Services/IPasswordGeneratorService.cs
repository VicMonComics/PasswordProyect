namespace PasswordSave.Services.Abstractions;

public sealed record PasswordGenerationOptions(
    int Length,
    bool UseUppercase,
    bool UseLowercase,
    bool UseNumbers,
    bool UseSymbols);

/// <summary>Nivel de fortaleza estimado de una contraseña generada, para mostrar en la barra visual.</summary>
public enum PasswordStrength
{
    Debil,
    Aceptable,
    Segura,
    MuySegura
}

public interface IPasswordGeneratorService
{
    /// <summary>Genera una contraseña aleatoria criptográficamente segura según las opciones dadas.</summary>
    string Generate(PasswordGenerationOptions options);

    /// <summary>Estima la fortaleza de una contraseña ya generada con estas opciones (para la barra visual).</summary>
    PasswordStrength EstimateStrength(PasswordGenerationOptions options);
}
