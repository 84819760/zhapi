namespace ZhApi.WpfApp.LogItems;

[AddService(ServiceLifetime.Transient)]
public partial class HomeAndStopViewModel(CancellationTokenSource cts) : ObservableObject
{
    public HomeAndStopViewModel() : this(null!) { }

    [ObservableProperty]
    public partial bool StopEnabled { get; set; } = true;

    [ObservableProperty]
    public partial Visibility Home { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility Stop { get; set; } = Visibility.Visible;

    [ObservableProperty]
    public partial Visibility ControlVisibility { get; set; } = Visibility.Visible;

    public CancellationToken Token { get; } = cts.Token;


    [RelayCommand]
    public void CallHome()
    {
        App.Home();
        App.Taskbar.StopIndeterminate();
        StopEnabled = false;
    }

    [RelayCommand]
    public Task CallStop()
    {
        this.AppInvoke(() => StopEnabled = false);
        return Task.Run(cts.Cancel);
    }

    public void ShowHome()
    {
        Stop = Visibility.Collapsed;
        Home = Visibility.Visible;
    }
}