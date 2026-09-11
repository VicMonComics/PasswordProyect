using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordSave.Common;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private readonly ICredentialRepository _credentialRepository;

    private static readonly string[] ChipOptions = { "Todos", "Favoritos", "Trabajo", "Redes" };

    private IReadOnlyList<CredentialListItem> _allCredentials = Array.Empty<CredentialListItem>();
    private string _searchText = string.Empty;
    private string _selectedChip = ChipOptions[0];
    private bool _isVaultEmpty;
    private bool _isFilterEmpty;
    private bool _hasResults;
    private bool _isBusy;
    public HomeViewModel(ICredentialRepository credentialRepository)
    {
        _credentialRepository = credentialRepository;

        Chips = new ObservableCollection<ChipItem>(
            ChipOptions.Select(c => new ChipItem(c, c == _selectedChip)));

        Credentials = new ObservableCollection<CredentialListItem>();
        SelectChipCommand = new RelayCommand<string>(SelectChip);
        OpenCredentialCommand = new RelayCommand<string>(id => OpenCredentialRequested?.Invoke(this, id ?? string.Empty));
    }

    /// <summary>Se dispara cuando el usuario toca una fila — la página se encarga de navegar al detalle/edición.</summary>
    public event EventHandler<string>? OpenCredentialRequested;

    public ObservableCollection<CredentialListItem> Credentials { get; }
    public ObservableCollection<ChipItem> Chips { get; }
    public ICommand SelectChipCommand { get; }
    public ICommand OpenCredentialCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    /// <summary>True si el vault no tiene ninguna credencial guardada (primera vez).</summary>
    public bool IsVaultEmpty
    {
        get => _isVaultEmpty;
        private set => SetProperty(ref _isVaultEmpty, value);
    }

    /// <summary>True si hay credenciales guardadas, pero el filtro/búsqueda actual no encontró ninguna.</summary>
    public bool IsFilterEmpty
    {
        get => _isFilterEmpty;
        private set => SetProperty(ref _isFilterEmpty, value);
    }

    /// <summary>True si hay al menos un resultado que mostrar en la lista.</summary>
    public bool HasResults
    {
        get => _hasResults;
        private set => SetProperty(ref _hasResults, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Se llama desde HomePage.OnAppearing — recarga por si se agregó/editó/eliminó algo desde otra pantalla.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var entries = await _credentialRepository.GetAllAsync();
            _allCredentials = entries.Select(CredentialDisplayMapper.ToListItem).ToList();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SelectChip(string? chip)
    {
        if (chip is null || chip == _selectedChip)
            return;

        _selectedChip = chip;

        foreach (var item in Chips)
            item.IsSelected = item.Label == chip;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allCredentials.Where(MatchesFilter);

        Credentials.Clear();
        foreach (var credential in filtered)
            Credentials.Add(credential);

        IsVaultEmpty = _allCredentials.Count == 0;
        IsFilterEmpty = !IsVaultEmpty && Credentials.Count == 0;
        HasResults = Credentials.Count > 0;
    }

    private bool MatchesFilter(CredentialListItem item)
    {
        var matchesChip = _selectedChip switch
        {
            "Todos" => true,
            "Favoritos" => item.IsFavorite,
            _ => item.Category == _selectedChip
        };

        var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
            || item.SiteName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesChip && matchesSearch;
    }
}

public sealed class ChipItem : ObservableObject
{
    private bool _isSelected;

    public ChipItem(string label, bool isSelected)
    {
        Label = label;
        _isSelected = isSelected;
    }

    public string Label { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
