namespace PasswordSave.ViewModels;

/// <summary>Una tarjeta de la pantalla de Categorías, con el conteo real de credenciales y si está bloqueada por Premium.</summary>
public sealed class CategoryCardItem
{
    public required string Name { get; init; }
    public required int Count { get; init; }
    public required bool IsLocked { get; init; }
}
