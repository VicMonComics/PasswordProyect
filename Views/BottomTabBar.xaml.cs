using CommunityToolkit.Maui.Behaviors;

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
        var inactiveColor = Colors.White;

        SetTabColor(InicioButton, InicioImage, "Inicio", activeColor, inactiveColor);
        SetTabColor(GeneradorButton, GeneradorImage, "Generador", activeColor, inactiveColor);
        SetTabColor(CategoriasButton, CategoriasImage, "Categorías", activeColor, inactiveColor);
        SetTabColor(AjustesButton, AjustesImage, "Ajustes", activeColor, inactiveColor);
    }

    private static void SetTabColor(
        VerticalStackLayout tab,
        Image image,
        string tabName,
        Color activeColor,
        Color inactiveColor)
    {
        var label = tab.Children.OfType<Label>().FirstOrDefault();
        var isActive = tabName == ((BottomTabBar)tab.Parent!.Parent!).ActiveTab;

        if (label != null)
            label.TextColor = isActive ? activeColor : inactiveColor;

        var behavior = image.Behaviors.OfType<IconTintColorBehavior>().FirstOrDefault();

        if (behavior != null)
            behavior.TintColor = isActive ? activeColor : inactiveColor;
    }
}