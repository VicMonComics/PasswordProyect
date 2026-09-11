namespace PasswordSave.Common;

public partial class BottomTabBar : ContentView
{
    public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
        nameof(ActiveTab), typeof(string), typeof(BottomTabBar), "Inicio", propertyChanged: OnActiveTabChanged);

    public event EventHandler<string>? TabSelected;

    public BottomTabBar()
    {
        InitializeComponent();
        UpdateHighlight();
    }

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    private static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((BottomTabBar)bindable).UpdateHighlight();
    }

    private void OnTabClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        var tab = button.Text;
        if (tab == ActiveTab)
            return; // ya estamos en esa pestaña, no hay nada que navegar

        TabSelected?.Invoke(this, tab);
    }

    private void UpdateHighlight()
    {
        var activeColor = (Color)Application.Current!.Resources["Jade"];
        var inactiveColor = (Color)Application.Current!.Resources["TextTertiary"];

        foreach (var button in new[] { InicioButton, GeneradorButton, CategoriasButton, AjustesButton })
            button.TextColor = button.Text == ActiveTab ? activeColor : inactiveColor;
    }
}
