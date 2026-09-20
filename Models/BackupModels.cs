using System.Collections.Generic;

namespace PasswordSave.Models;

/// <summary>
/// Forma del archivo de backup en disco (JSON). Todo lo sensible viaja en
/// Base64 pero YA cifrado (WrappedVaultKey con la contraseña maestra;
/// cada campo de cada entrada con la vault key) — el JSON en sí no es una
/// capa de seguridad, es solo el empaquetado.
/// </summary>
public sealed class BackupFile
{
    public int FormatVersion { get; init; } = 1;
    public required string Salt { get; init; }
    public required int Iterations { get; init; }
    public required string WrappedVaultKey { get; init; }
    public List<BackupEntry> Entries { get; init; } = new();
}

public sealed class BackupEntry
{
    public required string Id { get; init; }
    public required string SiteName { get; init; }
    public required string Category { get; init; }
    public bool IsFavorite { get; init; }
    public required string UsernameEncrypted { get; init; }
    public required string PasswordEncrypted { get; init; }
    public required string WebsiteEncrypted { get; init; }
    public required string NotesEncrypted { get; init; }
}
