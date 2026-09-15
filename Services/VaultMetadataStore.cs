using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Implementación de <see cref="IVaultMetadataStore"/> sobre SQLite local
/// (Microsoft.Data.Sqlite: el proveedor ADO.NET oficial de Microsoft, no
/// una librería de cifrado de terceros — la base en sí no cifra nada, solo
/// guarda bytes ya cifrados por EncryptionService/VaultKeyService).
///
/// El salt y las iteraciones viven en texto plano en esta tabla a propósito
/// (no son secretos: sin la contraseña maestra no sirven de nada). Lo único
/// sensible acá es la columna wrapped_vault_key, y esa ya llega cifrada.
/// </summary>
public sealed class VaultMetadataStore : IVaultMetadataStore
{
    private static string ConnectionString => DatabasePaths.ConnectionString;

    public async Task<bool> VaultExistsAsync()
    {
        await EnsureTableExistsAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM vault_metadata WHERE id = 1;";
        var count = (long)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? 0L);
        return count > 0;
    }

    public async Task SaveVaultAsync(KdfParameters kdfParameters, EncryptedPayload wrappedVaultKey)
    {
        ArgumentNullException.ThrowIfNull(kdfParameters);
        ArgumentNullException.ThrowIfNull(wrappedVaultKey);

        await EnsureTableExistsAsync().ConfigureAwait(false);

        if (await VaultExistsAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Ya existe un vault creado en este dispositivo.");

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO vault_metadata (id, salt, iterations, wrapped_vault_key)
            VALUES (1, $salt, $iterations, $wrappedVaultKey);
            """;
        command.Parameters.AddWithValue("$salt", kdfParameters.Salt);
        command.Parameters.AddWithValue("$iterations", kdfParameters.Iterations);
        command.Parameters.AddWithValue("$wrappedVaultKey", wrappedVaultKey.ToBytes());

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task UpdateVaultAsync(KdfParameters kdfParameters, EncryptedPayload wrappedVaultKey)
    {
        ArgumentNullException.ThrowIfNull(kdfParameters);
        ArgumentNullException.ThrowIfNull(wrappedVaultKey);

        await EnsureTableExistsAsync().ConfigureAwait(false);

        if (!await VaultExistsAsync().ConfigureAwait(false))
            throw new InvalidOperationException("No hay ningún vault creado todavía en este dispositivo.");

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE vault_metadata
            SET salt = $salt, iterations = $iterations, wrapped_vault_key = $wrappedVaultKey
            WHERE id = 1;
            """;
        command.Parameters.AddWithValue("$salt", kdfParameters.Salt);
        command.Parameters.AddWithValue("$iterations", kdfParameters.Iterations);
        command.Parameters.AddWithValue("$wrappedVaultKey", wrappedVaultKey.ToBytes());

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task<KdfParameters> GetKdfParametersAsync()
    {
        await EnsureTableExistsAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT salt, iterations FROM vault_metadata WHERE id = 1;";

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
            throw new InvalidOperationException("No hay ningún vault creado todavía en este dispositivo.");

        var salt = (byte[])reader["salt"];
        var iterations = reader.GetInt32(reader.GetOrdinal("iterations"));

        return new KdfParameters(salt, iterations);
    }

    public async Task<EncryptedPayload> GetWrappedVaultKeyAsync()
    {
        await EnsureTableExistsAsync().ConfigureAwait(false);

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT wrapped_vault_key FROM vault_metadata WHERE id = 1;";

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
            throw new InvalidOperationException("No hay ningún vault creado todavía en este dispositivo.");

        var bytes = (byte[])reader["wrapped_vault_key"];
        return EncryptedPayload.FromBytes(bytes);
    }

    private static async Task EnsureTableExistsAsync()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        // id fijo en 1: por ahora hay un solo vault por dispositivo (no
        // multi-usuario). salt/iterations en claro a propósito (ver
        // comentario de la clase); wrapped_vault_key ya llega cifrada.
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS vault_metadata (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                salt BLOB NOT NULL,
                iterations INTEGER NOT NULL,
                wrapped_vault_key BLOB NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
