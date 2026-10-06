using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Services.Abstractions;
using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class AddEditCredentialPage : ContentPage
{
    private readonly AddEditCredentialViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private string? _pendingEntryId;
    private string? _pendingPrefilledPassword;

    public AddEditCredentialPage(AddEditCredentialViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
        _viewModel.Closed += OnClosed;
    }

    /// <summary>
    /// Llamar justo después de resolver la página vía DI y antes de
    /// navegar a ella. entryId null = modo "agregar"; con valor = "editar".
    /// prefilledPassword solo aplica en modo "agregar" (p. ej. viniendo del generador).
    /// </summary>
    public void Initialize(string? entryId, string? prefilledPassword = null)
    {
        _pendingEntryId = entryId;
        _pendingPrefilledPassword = prefilledPassword;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Esta pantalla no pasa por TabNavigationHelper (no es parte del
        // tabbar), así que necesita su propio registro de actividad —
        // sobre todo porque aquí el usuario puede pasar varios minutos
        // escribiendo notas largas sin tocar nada más.
        _serviceProvider.GetRequiredService<IAppLockService>().RecordActivity();
        await _viewModel.InitializeAsync(_pendingEntryId, _pendingPrefilledPassword);
    }

    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Navigation.PopAsync();

    private async void OnClosed(object? sender, EventArgs e) =>
        await Navigation.PopAsync();

    private async void OnViewPremiumClicked(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(_serviceProvider.GetRequiredService<StorePage>());
}
