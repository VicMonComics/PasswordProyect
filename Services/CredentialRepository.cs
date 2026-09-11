using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Implementación de <see cref="ICredentialRepository"/>. site_name y
/// category quedan en claro (se necesitan para listar/buscar sin descifrar
/// cada fila); username/password/website/notes se cifran por separado con
/// AES-256-GCM usando la vault key de la sesión actual.
///
/// Cada campo cifrado usa como associatedData "{id}:{nombre del campo}" —
/// no solo el id. Esto evita que alguien con acceso directo al archivo
/// SQLite pueda mover un ciphertext de un campo a otro (p. ej. copiar el
/// "password_encrypted" de una fila al "notes_encrypted" de otra, o incluso
/// entre columnas de la misma fila): al descifrar, el associatedData no
/// coincide y GCM rechaza el dato como manipulado.
/// </summary>
public sealed class CredentialRepository : ICredentialRepository
{
    private readonly IEncryptionService _encryptionService;
    private readonly IVaultSessionService _vaultSessionService;

    public CredentialRepository(IEncryptionService encryptionService, IVaultSessionService vaultSessionService)
    {
        _encryptionService = encryptionService;
        _vaultSessionService = vaultSessionService;
    }

    public async Task<IReadOnlyList<CredentialEntry>> GetAllAsync()
    {
        var vaultKey = RequireVaultKey();
        await EnsureTableExistsAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, site_name, category, is_favorite, username_encrypted, password_encrypted, website_encrypted, notes_encrypted
            FROM password_entries
            ORDER BY site_name COLLATE NOCASE;
            """;

        var results = new List<CredentialEntry>();
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
            results.Add(ReadEntry(reader, vaultKey));

        return results;
    }

    public async Task<CredentialEntry?> GetByIdAsync(string id)
    {
        var vaultKey = RequireVaultKey();
        await EnsureTableExistsAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, site_name, category, is_favorite, username_encrypted, password_encrypted, website_encrypted, notes_encrypted
            FROM password_entries
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        return await reader.ReadAsync().ConfigureAwait(false) ? ReadEntry(reader, vaultKey) : null;
    }

    public async Task SaveAsync(CredentialEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var vaultKey = RequireVaultKey();
        await EnsureTableExistsAsync().ConfigureAwait(false);

        var usernameBytes = Encrypt(entry.Username, entry.Id, "username", vaultKey);
        var passwordBytes = Encrypt(entry.Password, entry.Id, "password", vaultKey);
        var websiteBytes = Encrypt(entry.Website, entry.Id, "website", vaultKey);
        var notesBytes = Encrypt(entry.Notes, entry.Id, "notes", vaultKey);

        await using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO password_entries (id, site_name, category, is_favorite, username_encrypted, password_encrypted, website_encrypted, notes_encrypted, created_at, updated_at)
            VALUES ($id, $siteName, $category, $isFavorite, $username, $password, $website, $notes, $now, $now)
            ON CONFLICT(id) DO UPDATE SET
                site_name = excluded.site_name,
                category = excluded.category,
                is_favorite = excluded.is_favorite,
                username_encrypted = excluded.username_encrypted,
                password_encrypted = excluded.password_encrypted,
                website_encrypted = excluded.website_encrypted,
                notes_encrypted = excluded.notes_encrypted,
                updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$id", entry.Id);
        command.Parameters.AddWithValue("$siteName", entry.SiteName);
        command.Parameters.AddWithValue("$category", entry.Category);
        command.Parameters.AddWithValue("$isFavorite", entry.IsFavorite ? 1 : 0);
        command.Parameters.AddWithValue("$username", usernameBytes);
        command.Parameters.AddWithValue("$password", passwordBytes);
        command.Parameters.AddWithValue("$website", websiteBytes);
        command.Parameters.AddWithValue("$notes", notesBytes);
        command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        RequireVaultKey(); // solo para exigir sesión desbloqueada, consistente con el resto de los métodos
        await EnsureTableExistsAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM password_entries WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private CredentialEntry ReadEntry(SqliteDataReader reader, byte[] vaultKey)
    {
        var id = reader.GetString(reader.GetOrdinal("id"));

        return new CredentialEntry
        {
            Id = id,
            SiteName = reader.GetString(reader.GetOrdinal("site_name")),
            Category = reader.GetString(reader.GetOrdinal("category")),
            IsFavorite = reader.GetInt32(reader.GetOrdinal("is_favorite")) != 0,
            Username = Decrypt((byte[])reader["username_encrypted"], id, "username", vaultKey),
            Password = Decrypt((byte[])reader["password_encrypted"], id, "password", vaultKey),
            Website = Decrypt((byte[])reader["website_encrypted"], id, "website", vaultKey),
            Notes = Decrypt((byte[])reader["notes_encrypted"], id, "notes", vaultKey),
        };
    }

    private byte[] Encrypt(string plainText, string id, string fieldName, byte[] vaultKey)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
        var associatedData = Encoding.UTF8.GetBytes($"{id}:{fieldName}");
        return _encryptionService.Encrypt(plainBytes, vaultKey, associatedData).ToBytes();
    }

    private string Decrypt(byte[] storedBytes, string id, string fieldName, byte[] vaultKey)
    {
        var payload = Models.EncryptedPayload.FromBytes(storedBytes);
        var associatedData = Encoding.UTF8.GetBytes($"{id}:{fieldName}");
        var plainBytes = _encryptionService.Decrypt(payload, vaultKey, associatedData);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] RequireVaultKey()
    {
        return _vaultSessionService.CurrentVaultKey
            ?? throw new InvalidOperationException("El vault está bloqueado: no se puede leer ni guardar contraseñas sin desbloquear primero.");
    }

    private static async Task EnsureTableExistsAsync()
    {
        await using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS password_entries (
                id TEXT PRIMARY KEY,
                site_name TEXT NOT NULL,
                category TEXT NOT NULL,
                is_favorite INTEGER NOT NULL DEFAULT 0,
                username_encrypted BLOB NOT NULL,
                password_encrypted BLOB NOT NULL,
                website_encrypted BLOB NOT NULL,
                notes_encrypted BLOB NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
