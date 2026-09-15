using Microsoft.Extensions.DependencyInjection;
using PasswordSave.Common;
using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public CategoriesPage(CategoriesViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
        _viewModel.LockedCategoryTapped += OnLockedCategoryTapped;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnTabSelected(object? sender, string tab) =>
        await TabNavigationHelper.NavigateToTabAsync(Navigation, _serviceProvider, tab, this);

    private async void OnViewPremiumClicked(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(_serviceProvider.GetRequiredService<StorePage>());

    private async void OnLockedCategoryTapped(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(_serviceProvider.GetRequiredService<StorePage>());
}
