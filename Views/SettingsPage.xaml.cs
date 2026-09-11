using PasswordSave.Common;

namespace PasswordSave.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public SettingsPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnTabSelected(object? sender, string tab) =>
        await TabNavigationHelper.NavigateToTabAsync(Navigation, _serviceProvider, tab, this);
}
