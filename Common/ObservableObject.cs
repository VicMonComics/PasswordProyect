using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PasswordSave.Common;

/// <summary>
/// Base mínima para ViewModels: implementa INotifyPropertyChanged a mano
/// para no depender de CommunityToolkit.Mvvm ni de ningún otro NuGet.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
