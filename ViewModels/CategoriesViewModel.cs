using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordSave.Common;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.ViewModels;

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPremiumStatusService _premiumStatusService;

    private bool _isPremium;

    public CategoriesViewModel(ICredentialRepository credentialRepository, IPremiumStatusService premiumStatusService)
    {
        _credentialRepository = credentialRepository;
        _premiumStatusService = premiumStatusService;

        OpenCategoryCommand = new RelayCommand<CategoryCardItem>(OpenCategory);
    }

    /// <summary>Se dispara solo cuando el usuario toca una categoría bloqueada — la página navega al paywall.</summary>
    public event EventHandler? LockedCategoryTapped;

    public ObservableCollection<CategoryCardItem> Categories { get; } = new();
    public ICommand OpenCategoryCommand { get; }

    public bool IsPremium
    {
        get => _isPremium;
        private set => SetProperty(ref _isPremium, value);
    }

    public async Task LoadAsync()
    {
        IsPremium = await _premiumStatusService.IsPremiumAsync();

        var entries = await _credentialRepository.GetAllAsync();
        var countsByCategory = entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        Categories.Clear();
        foreach (var categoryName in AddEditCredentialViewModel.CategoryOptions)
        {
            var isLocked = !IsPremium && !PremiumLimits.FreeCategories.Contains(categoryName);
            var count = countsByCategory.TryGetValue(categoryName, out var c) ? c : 0;

            Categories.Add(new CategoryCardItem
            {
                Name = categoryName,
                Count = count,
                IsLocked = isLocked,
            });
        }
    }

    private void OpenCategory(CategoryCardItem? category)
    {
        if (category is { IsLocked: true })
            LockedCategoryTapped?.Invoke(this, EventArgs.Empty);

        // Categorías desbloqueadas: por ahora no navegan a nada (el filtro
        // de Inicio por categoría específica queda para una siguiente
        // pasada); el prototipo tampoco definía esa interacción.
    }
}
