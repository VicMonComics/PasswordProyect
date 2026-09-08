using System.Threading.Tasks;
using PasswordSave.Models;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Genera y protege la "vault key": la clave aleatoria de 256 bits que cifra
/// directamente cada contraseña guardada. Esta clave nunca cambia mientras el
/// vault exista (aunque el usuario cambie su contraseña maestra) — lo único
/// que cambia es la forma en que está envuelta/protegida. Esto permite
/// cambiar la contraseña maestra sin tener que re-cifrar cada entrada.
/// </summary>
public interface IVaultKeyService
{
    /// <summary>Genera una nueva vault key aleatoria (256 bits). Se usa una sola vez, al crear el vault.</summary>
    byte[] GenerateVaultKey();

    /// <summary>Envuelve (cifra) la vault key usando la master key derivada de la contraseña maestra.</summary>
    EncryptedPayload WrapWithMasterKey(byte[] vaultKey, byte[] masterKey);

    /// <summary>Desenvuelve (descifra) la vault key usando la master key. Lanza si la contraseña es incorrecta.</summary>
    byte[] UnwrapWithMasterKey(EncryptedPayload wrappedVaultKey, byte[] masterKey);

    /// <summary>
    /// Guarda una copia de la vault key en el almacenamiento seguro del sistema
    /// (Android Keystore / iOS Keychain, vía SecureStorage de MAUI) para el
    /// desbloqueo rápido con PIN/biometría. La verificación de PIN/biometría
    /// en sí es responsabilidad de AppLockService; este método solo persiste
    /// el material una vez que ese acceso ya fue autorizado.
    /// </summary>
    Task StoreForQuickUnlockAsync(byte[] vaultKey);

    /// <summary>Recupera la vault key guardada para desbloqueo rápido, o null si no existe (primer uso, o fue borrada).</summary>
    Task<byte[]?> TryGetQuickUnlockKeyAsync();

    /// <summary>
    /// Borra la copia de desbloqueo rápido. Debe llamarse al cerrar sesión
    /// explícitamente, al cambiar la contraseña maestra, o al superar el
    /// límite de intentos fallidos de PIN/biometría.
    /// </summary>
    Task ClearQuickUnlockAsync();
}
