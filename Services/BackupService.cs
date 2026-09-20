using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

public sealed class BackupService : IBackupService
{
    private readonly IVaultMetadataStore _vaultMetadataStore;
    private readonly ICredentialRepository _credentialRepository;

    public BackupService(IVaultMetadataStore vaultMetadataStore, ICredentialRepository credentialRepository)
    {
        _vaultMetadataStore = vaultMetadataStore;
        _credentialRepository = credentialRepository;
    }

    public async Task<string> CreateBackupFileAsync()
    {
        var kdfParameters = await _vaultMetadataStore.GetKdfParametersAsync();
        var wrappedVaultKey = await _vaultMetadataStore.GetWrappedVaultKeyAsync();
        var rawRows = await _credentialRepository.GetAllRawAsync();

        var backup = new BackupFile
        {
            Salt = Convert.ToBase64String(kdfParameters.Salt),
            Iterations = kdfParameters.Iterations,
            WrappedVaultKey = Convert.ToBase64String(wrappedVaultKey.ToBytes()),
            Entries = rawRows.Select(r => new BackupEntry
            {
                Id = r.Id,
                SiteName = r.SiteName,
                Category = r.Category,
                IsFavorite = r.IsFavorite,
                UsernameEncrypted = Convert.ToBase64String(r.UsernameEncrypted),
                PasswordEncrypted = Convert.ToBase64String(r.PasswordEncrypted),
                WebsiteEncrypted = Convert.ToBase64String(r.WebsiteEncrypted),
                NotesEncrypted = Convert.ToBase64String(r.NotesEncrypted),
            }).ToList(),
        };

        var json = JsonSerializer.Serialize(backup);

        // Nombre disimulado a propósito (decisión ya tomada hace tiempo):
        // no es la protección real — eso es el cifrado — solo discreción
        // para quien vea el celular por encima.
        var fileName = $"img_cache_{Guid.NewGuid():N}.dat";
        var filePath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);

        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        return filePath;
    }

    public async Task RestoreBackupAsync(string filePath)
    {
        if (await _vaultMetadataStore.VaultExistsAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Ya existe un vault en este dispositivo. Restaurar un backup solo aplica en un dispositivo nuevo, antes de crear una cuenta.");

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("No se pudo leer el archivo seleccionado.", ex);
        }

        BackupFile? backup;
        try
        {
            backup = JsonSerializer.Deserialize<BackupFile>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("El archivo no es un backup válido de PasswordSave.", ex);
        }

        if (backup is null || string.IsNullOrEmpty(backup.WrappedVaultKey) || string.IsNullOrEmpty(backup.Salt))
            throw new InvalidOperationException("El archivo no es un backup válido de PasswordSave.");

        var kdfParameters = new KdfParameters(Convert.FromBase64String(backup.Salt), backup.Iterations);
        var wrappedVaultKey = EncryptedPayload.FromBytes(Convert.FromBase64String(backup.WrappedVaultKey));

        await _vaultMetadataStore.SaveVaultAsync(kdfParameters, wrappedVaultKey).ConfigureAwait(false);

        var rows = backup.Entries.Select(e => new RawCredentialRow
        {
            Id = e.Id,
            SiteName = e.SiteName,
            Category = e.Category,
            IsFavorite = e.IsFavorite,
            UsernameEncrypted = Convert.FromBase64String(e.UsernameEncrypted),
            PasswordEncrypted = Convert.FromBase64String(e.PasswordEncrypted),
            WebsiteEncrypted = Convert.FromBase64String(e.WebsiteEncrypted),
            NotesEncrypted = Convert.FromBase64String(e.NotesEncrypted),
        }).ToList();

        await _credentialRepository.ReplaceAllRawAsync(rows).ConfigureAwait(false);
    }
}
