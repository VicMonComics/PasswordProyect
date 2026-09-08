using System.Threading.Tasks;

namespace PasswordSave.Services.Abstractions;

/// <summary>
/// Envuelve el plugin de terceros (Plugin.Maui.Biometric) detrás de nuestra
/// propia interfaz: si el plugin cambia de API o hay que migrarlo, solo se
/// toca <see cref="Services.DeviceBiometricService"/>, no cada pantalla que
/// usa biometría.
/// </summary>
public interface IDeviceBiometricService
{
    /// <summary>True si el dispositivo tiene un sensor biométrico configurado y disponible ahora mismo.</summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Muestra el prompt biométrico nativo del SO. Devuelve true solo si el
    /// usuario fue autenticado; false para cualquier otro caso (canceló, no
    /// disponible, error, timeout) — a quien llama no le importa el motivo
    /// exacto, solo si puede continuar o debe caer a PIN/contraseña.
    /// </summary>
    Task<bool> AuthenticateAsync(string reason);
}