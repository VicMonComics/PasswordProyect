using System.IO;
using Microsoft.Maui.Storage;

namespace PasswordSave.Services;

internal static class DatabasePaths
{
    private const string DatabaseFileName = "passwordsave.db3";

    public static string DatabasePath =>
        Path.Combine(FileSystem.Current.AppDataDirectory, DatabaseFileName);

    public static string ConnectionString => $"Data Source={DatabasePath}";
}
