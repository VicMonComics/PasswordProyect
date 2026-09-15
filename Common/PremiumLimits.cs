namespace PasswordSave.Common;

/// <summary>
/// Límites del plan Free. Si en algún momento cambias de opinión sobre
/// cualquiera de estos valores, este es el único archivo que hay que tocar
/// — el resto de la app (AddEditCredentialViewModel, CategoriesViewModel)
/// solo lee de aquí, nunca tiene el número escrito a mano.
/// </summary>
public static class PremiumLimits
{
    /// <summary>
    /// Máximo de credenciales guardadas permitidas en el plan Free.
    /// Premium no tiene límite. Decidido en septiembre 2026: 15.
    /// </summary>
    public const int FreeTierCredentialLimit = 15;

    /// <summary>
    /// Categorías disponibles sin membresía Premium. El resto de
    /// AddEditCredentialViewModel.CategoryOptions queda bloqueado.
    /// </summary>
    public static readonly string[] FreeCategories = { "General" };
}
