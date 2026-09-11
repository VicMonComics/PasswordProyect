using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Common;

namespace PasswordSave.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public CategoriesPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnTabSelected(object? sender, string tab) =>
        await TabNavigationHelper.NavigateToTabAsync(Navigation, _serviceProvider, tab, this);

    private async void OnViewPremiumClicked(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(_serviceProvider.GetRequiredService<PaywallPage>());
}
