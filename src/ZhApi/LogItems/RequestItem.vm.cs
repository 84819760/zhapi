namespace ZhApi.WpfApp.LogItems;
public partial class RequestItemViewModel : ControlProvider
{
    private readonly DateTime dateTime = DateTime.Now;
    private volatile bool isStop = false;

    public RequestItemViewModel() => SetTime();

    protected override Control CreateControl() => new RequestItem() { DataContext = this };

    [ObservableProperty]
    public partial Guid Gid { get; set; }

    [ObservableProperty]
    public partial int RequestLength { get; set; }

    [ObservableProperty]
    public partial int RetryIndex { get; set; }

    public string ToolTip => $"请求长度:{RequestLength}，重试次数:{RetryIndex}";

    [ObservableProperty]
    public partial string? Time { get; set; } = "0s";

    public void Stop() => isStop = true;

    async void SetTime()
    {
        while (!isStop)
        {
            await Task.Delay(1000);
            var time = DateTime.Now - dateTime;
            Time = time.ForamtCropping();
        }
    }
}
