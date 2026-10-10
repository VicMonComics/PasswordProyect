using CommunityToolkit.Mvvm.ComponentModel;


namespace PasswordSave.ViewModels
{
    public partial class DisplayAlertCustomViewModel : BaseAlerta
    {

        private static DisplayAlertCustomViewModel instance = null;

        public static DisplayAlertCustomViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DisplayAlertCustomViewModel();

                }
                return instance;
            }
        }
        public enum messageType
        {
            Informacion = 1,
            Error,
            Confirmacion,
            Advertencia
        }

        [ObservableProperty]
        private string _title;

        private string _mensaje;

        public string Mensaje
        {
            get => _mensaje;
            set
            {
                SetProperty(ref _mensaje, value);
                OnPropertyChanged(nameof(AnimationPlaying));
            }
        }

        private messageType _messageType;
        public messageType MessageType
        {
            get => _messageType;
            set
            {
                SetProperty(ref _messageType, value);
                OnPropertyChanged(nameof(Ico));
            }

        }
        public bool AnimationPlaying { get => (string.IsNullOrEmpty(Mensaje) ? false : true); }

        public string Ico { get => (MessageType == messageType.Informacion) ? "llave.png" : (MessageType == messageType.Error) ? "cruz.png" : (MessageType == messageType.Confirmacion) ? "ok.png" : "info.png"; }

        public DisplayAlertCustomViewModel()
        {
        }
    }
}
