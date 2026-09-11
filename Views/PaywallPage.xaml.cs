namespace PasswordSave.Views;

public partial class PaywallPage : ContentPage
{
    public PaywallPage()
    {
        InitializeComponent();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();

    private async void OnStartTrialClicked(object? sender, EventArgs e)
    {
        // PENDIENTE: aquí se conecta la compra real (Google Play Billing /
        // Apple StoreKit) a través de IPremiumStatusService una vez que
        // decidamos el plugin de facturación — ver la nota que dejo abajo.
        await DisplayAlert("Próximamente", "La compra todavía no está conectada a la tienda.", "Entendido");
    }
}
