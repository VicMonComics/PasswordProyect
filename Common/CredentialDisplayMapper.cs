using System;
using PasswordSave.Models;

namespace PasswordSave.Common;

public static class CredentialDisplayMapper
{
    // Colores de avatar: se asigna uno de forma determinística según el
    // nombre del sitio (mismo sitio siempre cae en el mismo color).
    private static readonly (string Background, string TextOn)[] AvatarPalette =
    {
        ("#E5504E", "#EDF6F3"),
        ("#4C8DFF", "#EDF6F3"),
        ("#22C58B", "#062018"),
        ("#E4B45A", "#241804"),
        ("#8B6FE8", "#EDF6F3"),
        ("#28B8C4", "#062018"),
        ("#E0708E", "#EDF6F3"),
    };

    public static ViewModels.CredentialListItem ToListItem(CredentialEntry entry)
    {
        var palette = AvatarPalette[Math.Abs(entry.SiteName.GetHashCode()) % AvatarPalette.Length];
        var initial = entry.SiteName.Length > 0 ? entry.SiteName[..1].ToUpperInvariant() : "?";

        return new ViewModels.CredentialListItem
        {
            Id = entry.Id,
            SiteName = entry.SiteName,
            SubtitleMasked = MaskSubtitle(entry.Username),
            AvatarInitial = initial,
            AvatarColorHex = palette.Background,
            AvatarTextColorHex = palette.TextOn,
            Category = entry.Category,
            IsFavorite = entry.IsFavorite,
        };
    }

    private static string MaskSubtitle(string username)
    {
        if (string.IsNullOrEmpty(username))
            return string.Empty;

        var atIndex = username.IndexOf('@');
        if (atIndex > 0)
        {
            var local = username[..atIndex];
            var domain = username[atIndex..]; // incluye el "@"
            var visible = local.Length <= 3 ? local : local[..3];
            return $"{visible}•••{domain}";
        }

        return username.Length <= 4 ? $"{username}•••" : $"{username[..4]}•••";
    }
}
