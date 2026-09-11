namespace PasswordSave.ViewModels;

/// <summary>DTO de UI para una fila de la lista de Inicio — mapeado desde CredentialEntry.</summary>
public sealed class CredentialListItem
{
    public required string Id { get; init; }
    public required string SiteName { get; init; }
    public required string SubtitleMasked { get; init; }
    public required string AvatarInitial { get; init; }
    public required string AvatarColorHex { get; init; }
    public required string AvatarTextColorHex { get; init; }
    public required string Category { get; init; }
    public bool IsFavorite { get; init; }
}
