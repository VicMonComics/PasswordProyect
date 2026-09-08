using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using PasswordSave.Models;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Services;

/// <summary>
/// Implementación de <see cref="IAppLockService"/>. No conoce la vault key
/// ni el cifrado de las entradas — eso es trabajo de <see cref="IVaultKeyService"/>
/// y <see cref="IEncryptionService"/>. Este servicio solo decide CUÁNDO se
/// permite pedirla: valida el PIN, cuenta intentos fallidos, controla el
/// timeout de inactividad y avisa cuando hay que ocultar contenido en
/// segundo plano.
/// </summary>
public sealed class AppLockService : IAppLockService
{
    public const int MaxFailedAttempts = 5;
    private const int DefaultInactivityMinutes = 5;
    private const int MaxInactivityMinutesPremium = 10;

    // El PIN ya está protegido por el límite de intentos fallidos (no por la
    // fuerza de la KDF), pero seguimos exigiendo un mínimo defendible de
    // iteraciones para no debilitar el estándar general del método.
    private const int PinKdfIterations = 100_000;

    private const string PinHashStorageKey = "passwordsave.pin.hash";
    private const string FailedAttemptsKey = "passwordsave.pin.failedattempts";
    private const string InactivityMinutesKey = "passwordsave.lock.inactivityminutes";
    private const string LastActivityTicksKey = "passwordsave.lock.lastactivityticks";

    private readonly IKeyDerivationService _keyDerivationService;

    public AppLockService(IKeyDerivationService keyDerivationService)
    {
        _keyDerivationService = keyDerivationService ?? throw new ArgumentNullException(nameof(keyDerivationService));
    }

    public event EventHandler? Locked;
    public event EventHandler<bool>? ContentVisibilityChanged;

    public TimeSpan InactivityTimeout
    {
        get
        {
            var minutes = Preferences.Default.Get(InactivityMinutesKey, DefaultInactivityMinutes);
            return TimeSpan.FromMinutes(minutes);
        }
    }

    public async Task<bool> HasPinAsync()
    {
        var stored = await SecureStorage.Default.GetAsync(PinHashStorageKey).ConfigureAwait(false);
        return stored is not null;
    }

    public async Task SetPinAsync(string pin)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 6 || !IsAllDigits(pin))
            throw new ArgumentException("El PIN debe tener entre 4 y 6 dígitos.", nameof(pin));

        var parameters = _keyDerivationService.CreateParameters(iterations: PinKdfIterations);
        var hash = _keyDerivationService.DeriveKey(pin, parameters);

        var serialized = $"{parameters.Iterations}:{Convert.ToBase64String(parameters.Salt)}:{Convert.ToBase64String(hash)}";
        await SecureStorage.Default.SetAsync(PinHashStorageKey, serialized).ConfigureAwait(false);

        await ResetFailedAttemptsAsync().ConfigureAwait(false);
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        if (await IsPinLockedOutAsync().ConfigureAwait(false))
            return false;

        var stored = await SecureStorage.Default.GetAsync(PinHashStorageKey).ConfigureAwait(false);
        if (stored is null)
            return false;

        var parts = stored.Split(':');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var parameters = new KdfParameters(salt, iterations, expectedHash.Length);
        var actualHash = _keyDerivationService.DeriveKey(pin, parameters);

        // Comparación en tiempo constante: evita filtrar por timing cuántos
        // bytes del hash coinciden.
        var isValid = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);

        if (isValid)
            await ResetFailedAttemptsAsync().ConfigureAwait(false);
        else
            await RegisterFailedAttemptAsync().ConfigureAwait(false);

        return isValid;
    }

    public Task NotifyBiometricSuccessAsync() => ResetFailedAttemptsAsync();

    public Task<int> GetRemainingPinAttemptsAsync()
    {
        var failed = Preferences.Default.Get(FailedAttemptsKey, 0);
        return Task.FromResult(Math.Max(0, MaxFailedAttempts - failed));
    }

    public async Task<bool> IsPinLockedOutAsync()
    {
        var remaining = await GetRemainingPinAttemptsAsync().ConfigureAwait(false);
        return remaining <= 0;
    }

    public Task SetInactivityTimeoutAsync(TimeSpan timeout, bool isPremiumUser)
    {
        var maxAllowedMinutes = isPremiumUser ? MaxInactivityMinutesPremium : DefaultInactivityMinutes;
        var minutes = Math.Clamp((int)timeout.TotalMinutes, 1, maxAllowedMinutes);
        Preferences.Default.Set(InactivityMinutesKey, minutes);
        return Task.CompletedTask;
    }

    public void RecordActivity()
    {
        Preferences.Default.Set(LastActivityTicksKey, DateTime.UtcNow.Ticks);
    }

    public bool ShouldLockDueToInactivity()
    {
        var lastTicks = Preferences.Default.Get(LastActivityTicksKey, 0L);
        if (lastTicks == 0L)
            return false; // sin actividad registrada todavía (primer arranque)

        var lastActivity = new DateTime(lastTicks, DateTimeKind.Utc);
        return DateTime.UtcNow - lastActivity > InactivityTimeout;
    }

    public void OnAppBackgrounded()
    {
        // Se oculta de inmediato, sin esperar el timeout de inactividad:
        // protege contra la miniatura del selector de apps recientes.
        ContentVisibilityChanged?.Invoke(this, true);
        Preferences.Default.Set(LastActivityTicksKey, DateTime.UtcNow.Ticks);
    }

    public void OnAppForegrounded()
    {
        if (ShouldLockDueToInactivity())
        {
            Locked?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ContentVisibilityChanged?.Invoke(this, false);
            RecordActivity();
        }
    }

    public Task LockAsync()
    {
        Locked?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private Task ResetFailedAttemptsAsync()
    {
        Preferences.Default.Set(FailedAttemptsKey, 0);
        return Task.CompletedTask;
    }

    private Task RegisterFailedAttemptAsync()
    {
        var failed = Preferences.Default.Get(FailedAttemptsKey, 0) + 1;
        Preferences.Default.Set(FailedAttemptsKey, failed);
        return Task.CompletedTask;
    }

    private static bool IsAllDigits(string value)
    {
        foreach (var c in value)
        {
            if (c < '0' || c > '9')
                return false;
        }
        return true;
    }
}
