using System.Windows.Shell;
namespace ZhApi.WpfApp;

[AddService(ServiceLifetime.Singleton)]
public partial class TaskbarViewModel : ObservableRecipientActive
{
    [ObservableProperty]
    public partial TaskbarItemProgressState State { get; set; } = TaskbarItemProgressState.None;

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    public TaskbarItemInfo? TaskbarItemInfo { get; set; }

  
    public void Set(TaskbarItemProgressState state, double progress)
    {
        State = state;
        ProgressValue = progress;
    }


    public void StartIndeterminate()
    {
        State = TaskbarItemProgressState.Indeterminate;
    }

    public void StopIndeterminate()
    {
        State = TaskbarItemProgressState.None;
    }
}