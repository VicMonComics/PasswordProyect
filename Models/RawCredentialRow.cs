namespace PasswordSave.Models;

/// <summary>
/// Una fila de password_entries TAL CUAL está en SQLite, sin descifrar.
/// Solo la usa el backup: empacar/restaurar no necesita la vault key,
/// porque copia los bytes cifrados directo, sin tocarlos.
/// </summary>
public sealed class RawCredentialRow
{
    public required string Id { get; init; }
    public required string SiteName { get; init; }
    public required string Category { get; init; }
    public bool IsFavorite { get; init; }
    public required byte[] UsernameEncrypted { get; init; }
    public required byte[] PasswordEncrypted { get; init; }
    public required byte[] WebsiteEncrypted { get; init; }
    public required byte[] NotesEncrypted { get; init; }
}
