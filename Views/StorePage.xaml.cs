using PasswordSave.ViewModels;

namespace PasswordSave.Views;

public partial class StorePage : ContentPage
{
    private readonly StoreViewModel _viewModel;

    public StorePage(StoreViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
