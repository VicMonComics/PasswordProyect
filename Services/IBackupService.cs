using System.Threading.Tasks;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Empaqueta/restaura un backup completo del vault (parámetros KDF, vault
/// key envuelta con la contraseña maestra actual, y todas las credenciales
/// ya cifradas tal cual están en SQLite). No vuelve a cifrar nada — copia
/// los bytes que ya estaban cifrados.
/// </summary>
public interface IBackupService
{
    /// <summary>Genera el archivo de backup en la carpeta de caché de la app, listo para compartir. Devuelve la ruta completa.</summary>
    Task<string> CreateBackupFileAsync();

    /// <summary>
    /// Restaura el vault completo desde un archivo de backup. Solo
    /// funciona si en este dispositivo TODAVÍA no existe ningún vault
    /// (es decir, en un dispositivo nuevo, antes de crear una cuenta) —
    /// después de restaurar, el usuario inicia sesión normal con su
    /// contraseña maestra de siempre.
    /// </summary>
    Task RestoreBackupAsync(string filePath);
}
