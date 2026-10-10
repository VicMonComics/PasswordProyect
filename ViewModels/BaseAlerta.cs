using CommunityToolkit.Mvvm.ComponentModel;

namespace PasswordSave.ViewModels
{
    public partial class BaseAlerta : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;
        [ObservableProperty]
        private string _title;

        //public string Title { get => _title; set => _title = value; }
    }
}
