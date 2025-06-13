using ZhApi.Cores;

namespace ZhApi.WpfApp;
[AddService(ServiceLifetime.Singleton)]
public partial class MainWindowViewModel(MainControlViewModel mainControlViewModel,
    TaskbarViewModel taskbar, VersionHelper version) 
    : ObservableRecipientActive, IRecipient<ContentMessage>
{
    [ObservableProperty]
    public partial string? Titel { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Control))]
    public partial IControlProvider ControlViewModel { get; set; } = mainControlViewModel;

    public Control Control => ControlViewModel.Control;

    public TaskbarViewModel Taskbar { get; } = taskbar;

    public VersionHelper Version { get; } = version;

    public void Receive(ContentMessage message)
    {
        Titel = message.Message;
    }
}