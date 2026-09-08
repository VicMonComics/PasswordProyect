using System;
using System.Threading.Tasks;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Controla el acceso a la app: configuración y verificación del PIN, conteo
/// de intentos fallidos, timeout de inactividad, y el hook para ocultar
/// contenido cuando la app pasa a segundo plano. No dispara el prompt
/// biométrico nativo del SO (eso lo hace una implementación específica por
/// plataforma) — solo expone dónde conectar el resultado.
/// </summary>
public interface IAppLockService
{
    /// <summary>Tiempo de inactividad tras el cual se debe bloquear la app (5 min por defecto; hasta 10 si el usuario es premium).</summary>
    TimeSpan InactivityTimeout { get; }

    /// <summary>True si ya existe un PIN configurado.</summary>
    Task<bool> HasPinAsync();

    /// <summary>Define o cambia el PIN (4 a 6 dígitos). Resetea el contador de intentos fallidos.</summary>
    Task SetPinAsync(string pin);

    /// <summary>Verifica el PIN y actualiza el contador de intentos fallidos. Devuelve false también si ya está bloqueado por intentos.</summary>
    Task<bool> VerifyPinAsync(string pin);

    /// <summary>Se llama cuando la biometría del SO autoriza el acceso. Resetea el contador de intentos fallidos.</summary>
    Task NotifyBiometricSuccessAsync();

    /// <summary>Intentos fallidos de PIN restantes antes de exigir la contraseña maestra completa.</summary>
    Task<int> GetRemainingPinAttemptsAsync();

    /// <summary>True si se superó el límite de intentos: ya no se acepta PIN/biometría, solo la contraseña maestra.</summary>
    Task<bool> IsPinLockedOutAsync();

    /// <summary>
    /// Ajusta el timeout de inactividad. Máximo 10 minutos, y solo si
    /// <paramref name="isPremiumUser"/> es true; en caso contrario el
    /// máximo (y valor por defecto) es 5 minutos.
    /// </summary>
    Task SetInactivityTimeoutAsync(TimeSpan timeout, bool isPremiumUser);

    /// <summary>Marca actividad del usuario; reinicia el conteo de inactividad.</summary>
    void RecordActivity();

    /// <summary>True si pasó más tiempo del permitido desde la última actividad registrada.</summary>
    bool ShouldLockDueToInactivity();

    /// <summary>Llamar cuando el SO manda la app a segundo plano. Oculta el contenido de inmediato, sin esperar el timeout de inactividad.</summary>
    void OnAppBackgrounded();

    /// <summary>Llamar cuando la app vuelve a primer plano. Decide si re-mostrar el contenido o exigir desbloqueo por inactividad.</summary>
    void OnAppForegrounded();

    /// <summary>Fuerza el bloqueo inmediato (por ejemplo, un botón "bloquear ahora").</summary>
    Task LockAsync();

    /// <summary>Se dispara cuando la app debe pedir desbloqueo: por inactividad, bloqueo manual, o exceso de intentos fallidos.</summary>
    event EventHandler? Locked;

    /// <summary>
    /// Se dispara al entrar/salir de segundo plano para que la UI cubra u
    /// oculte el contenido sensible (protege la miniatura del selector de
    /// apps recientes). El argumento es true = ocultar.
    /// </summary>
    event EventHandler<bool>? ContentVisibilityChanged;
}
