namespace PasswordSave.Models;

/// <summary>
/// Una credencial guardada, ya descifrada — vive en memoria mientras el
/// vault está desbloqueado. Nunca se serializa así a disco: ICredentialRepository
/// se encarga de cifrar los campos sensibles antes de persistir.
/// </summary>
public sealed class CredentialEntry
{
    public required string Id { get; init; }

    // En claro en la base (ver ICredentialRepository): necesarios para
    // listar/buscar/filtrar sin tener que descifrar cada fila.
    public required string SiteName { get; set; }
    public required string Category { get; set; }
    public bool IsFavorite { get; set; }

    // Cifrados en la base con la vault key (AES-256-GCM, uno por campo).
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
