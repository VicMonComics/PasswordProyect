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
}
