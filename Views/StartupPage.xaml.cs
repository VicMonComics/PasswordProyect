using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Services.Abstractions;

namespace PasswordSave.Views;

public partial class StartupPage : ContentPage
{
    private readonly IVaultMetadataStore _vaultMetadataStore;
    private readonly IServiceProvider _serviceProvider;

    public StartupPage(IVaultMetadataStore vaultMetadataStore, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _vaultMetadataStore = vaultMetadataStore;
        _serviceProvider = serviceProvider;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var vaultExists = await _vaultMetadataStore.VaultExistsAsync();

        Page nextPage = vaultExists
            ? _serviceProvider.GetRequiredService<LoginPage>()
            : _serviceProvider.GetRequiredService<OnboardingPage>();

        await Navigation.PushAsync(nextPage);
        Navigation.RemovePage(this);
    }
}
