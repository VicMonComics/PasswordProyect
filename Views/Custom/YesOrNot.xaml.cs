using Mopups.Services;
using PasswordSave.ViewModels;

namespace PasswordSave.Views.Custom;

public partial class YesOrNot
{
    private static YesOrNot instance = null;
    public static YesOrNot Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new YesOrNot();
            }
            return instance;
        }
    }
    TaskCompletionSource<string> _taskCompletionSource;
    public string ReturnValue { get; set; }
    public Task<string> PopupDismissedTask => _taskCompletionSource.Task;
    public YesOrNot()
    {
        InitializeComponent();
        this.BindingContext = DisplayAlertCustomViewModel.Instance;
        NavigationPage.SetHasNavigationBar(this, false);
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _taskCompletionSource = new TaskCompletionSource<string>();

    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _taskCompletionSource.SetResult(ReturnValue);
    }
    void LoginButton_Clicked(System.Object sender, System.EventArgs e)
    {
        DisplayAlertCustomViewModel.Instance.Mensaje = string.Empty;
        MopupService.Instance.PopAsync();
    }

    private void YesBtn_Clicked(object sender, EventArgs e)
    {
        DisplayAlertCustomViewModel.Instance.Mensaje = string.Empty;
        ReturnValue = "SI";
        MopupService.Instance.PopAsync();
    }

    private void NoBtn_Clicked(object sender, EventArgs e)
    {
        DisplayAlertCustomViewModel.Instance.Mensaje = string.Empty;
        ReturnValue = "NO";
        MopupService.Instance.PopAsync();
    }
}