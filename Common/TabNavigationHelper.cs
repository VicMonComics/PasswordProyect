using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Views;

namespace PasswordSave.Common;

/// <summary>
/// Navega entre las 4 pantallas del tabbar inferior sin apilarlas (cada
/// cambio de pestaña reemplaza la pantalla actual, no la apila encima).
/// </summary>
public static class TabNavigationHelper
{
    public static async Task NavigateToTabAsync(INavigation navigation, IServiceProvider services, string tab, Page currentPage)
    {
        Page? nextPage = tab switch
        {
            "Inicio" => services.GetRequiredService<HomePage>(),
            "Generador" => services.GetRequiredService<GeneratorPage>(),
            "Categorías" => services.GetRequiredService<CategoriesPage>(),
            "Ajustes" => services.GetRequiredService<SettingsPage>(),
            _ => null
        };

        if (nextPage is null)
            return;

        await navigation.PushAsync(nextPage);
        navigation.RemovePage(currentPage);
    }
}
