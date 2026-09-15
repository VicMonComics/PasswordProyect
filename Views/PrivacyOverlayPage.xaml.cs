namespace PasswordSave.Views;

public partial class PrivacyOverlayPage : ContentPage
{
    public PrivacyOverlayPage()
    {
        InitializeComponent();
    }

    // Evita que el botón atrás de Android la quite mientras la app sigue en
    // segundo plano — solo App.xaml.cs (al recibir ContentVisibilityChanged)
    // debe poder cerrarla.
    protected override bool OnBackButtonPressed() => true;
}
