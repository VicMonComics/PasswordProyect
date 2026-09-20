using System.Threading.Tasks;
using System.Collections.Generic;
using PasswordSave.Models;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Lee y escribe las credenciales guardadas (tabla password_entries en
/// SQLite). Cifra/descifra usuario, contraseña, sitio web y notas con la
/// vault key de la sesión actual (IVaultSessionService) — si el vault está
/// bloqueado, cualquier método lanza InvalidOperationException.
/// </summary>
public interface ICredentialRepository
{
    Task<IReadOnlyList<CredentialEntry>> GetAllAsync();

    Task<CredentialEntry?> GetByIdAsync(string id);

    /// <summary>Inserta si el id no existe, actualiza si ya existe.</summary>
    Task SaveAsync(CredentialEntry entry);

    Task DeleteAsync(string id);

    /// <summary>
    /// Todas las filas TAL CUAL están en SQLite, sin descifrar. Solo para
    /// empacar un backup — a diferencia del resto de los métodos, no
    /// requiere el vault desbloqueado (no toca la vault key para nada).
    /// </summary>
    Task<IReadOnlyList<RawCredentialRow>> GetAllRawAsync();

    /// <summary>
    /// Reemplaza TODO el contenido de la tabla con estas filas (ya
    /// cifradas) — solo para restaurar un backup completo en un
    /// dispositivo nuevo. Borra cualquier fila que hubiera antes.
    /// </summary>
    Task ReplaceAllRawAsync(IReadOnlyList<RawCredentialRow> rows);
}
