namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Mantiene la vault key en memoria mientras la sesión está desbloqueada.
/// Vive solo en RAM — nunca se persiste aquí (para eso está
/// IVaultKeyService, que la envuelve para SecureStorage o para el backup).
/// Se limpia al bloquear la app (ver IAppLockService.Locked) o al cerrar
/// sesión explícitamente.
/// </summary>
public interface IVaultSessionService
{
    /// <summary>True si hay una vault key cargada en memoria ahora mismo.</summary>
    bool IsUnlocked { get; }

    /// <summary>La vault key actual, o null si la sesión está bloqueada.</summary>
    byte[]? CurrentVaultKey { get; }

    /// <summary>Guarda la vault key en memoria tras un login exitoso.</summary>
    void SetUnlocked(byte[] vaultKey);

    /// <summary>Borra la vault key de memoria (sobrescribe los bytes antes de soltarla).</summary>
    void Clear();
}
