using System;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Maui.Biometric;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Implementación sobre Plugin.Maui.Biometric — la única pieza de todo el
/// módulo de seguridad que depende de un NuGet de terceros. Es necesario:
/// llamar a BiometricPrompt (Android) / LocalAuthentication (iOS) desde .NET
/// requiere bindings nativos que no vienen en el BCL.
///
/// API verificada contra la wiki oficial del plugin (v0.1.x):
/// - GetAuthenticationStatusAsync devuelve BiometricHwStatus (no
///   "BiometricAuthenticationStatus", que no existe).
/// - AuthenticateAsync devuelve AuthenticationResponse, cuyo resultado se
///   lee en la propiedad Status (BiometricResponseStatus), no "Authenticated".
/// </summary>
public sealed class DeviceBiometricService : IDeviceBiometricService
{
    private readonly IBiometric _biometric;

    public DeviceBiometricService(IBiometric biometric)
    {
        _biometric = biometric ?? throw new ArgumentNullException(nameof(biometric));
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var status = await _biometric.GetAuthenticationStatusAsync().ConfigureAwait(false);
            return status == BiometricHwStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            var request = new AuthenticationRequest
            {
                Title = "PasswordSave",
                Subtitle = reason,
                Description = "Confirma tu identidad para desbloquear tu bóveda.",
                // AllowPasswordAuth=false: nuestro propio fallback es el PIN
                // (AppLockService), no el bloqueo de pantalla del sistema.
                // El plugin documenta que combinar AllowPasswordAuth=true
                // con NegativeText lanza excepción en Android.
                AllowPasswordAuth = false,
                NegativeText = "Usar PIN"
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _biometric.AuthenticateAsync(request, cts.Token).ConfigureAwait(false);
            return response.Status == BiometricResponseStatus.Success;
        }
        catch
        {
            // Cualquier falla (usuario canceló, hardware no disponible,
            // timeout) se trata igual: no autenticado, cae a PIN/contraseña.
            return false;
        }
    }
}
