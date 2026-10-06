namespace PasswordSave.Common;

public partial class BottomTabBar : ContentView
{
    public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
        nameof(ActiveTab),
        typeof(string),
        typeof(BottomTabBar),
        "Inicio",
        propertyChanged: OnActiveTabChanged);

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

    private void OnTabClicked(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string tab || tab == ActiveTab)
            return;

        ActiveTab = tab;
        TabSelected?.Invoke(this, tab);
    }

    private void UpdateHighlight()
    {
        var activeColor = (Color)Application.Current!.Resources["Jade"];
        var inactiveColor = (Color)Application.Current!.Resources["TextTertiary"];

        SetTabColor(InicioButton, "Inicio", activeColor, inactiveColor);
        SetTabColor(GeneradorButton, "Generador", activeColor, inactiveColor);
        SetTabColor(CategoriasButton, "Categorías", activeColor, inactiveColor);
        SetTabColor(AjustesButton, "Ajustes", activeColor, inactiveColor);
    }

    private static void SetTabColor(
        VerticalStackLayout tab,
        string tabName,
        Color activeColor,
        Color inactiveColor)
    {
        var label = tab.Children.OfType<Label>().FirstOrDefault();
        var image = tab.Children.OfType<Image>().FirstOrDefault();
        var isActive = tabName == ((BottomTabBar)tab.Parent.Parent).ActiveTab;

        if (label != null)
            label.TextColor = isActive ? activeColor : inactiveColor;

        if (image != null)
            image.Opacity = isActive ? 1.0 : 0.6;
    }
}