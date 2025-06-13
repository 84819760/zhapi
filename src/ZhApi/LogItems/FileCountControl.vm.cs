using ZhApi.Configs;
using ZhApi.Interfaces;

namespace ZhApi.WpfApp.LogItems;
[AddService(ServiceLifetime.Scoped)]
public partial class FileCountControlViewModel : ControlProviderMessage,
    IRecipient<IncreaseFile>, IRecipient<DecreaseFile>
{
    private readonly static Lock @lock = new();
    private readonly DateTime start = DateTime.Now;
    private readonly CancellationToken token;
    private bool isEnd;

    public FileCountControlViewModel() : base(null!) { }

    public FileCountControlViewModel(
        IOptionsSnapshot<AppConfig> appConfig,
        ITranslateService translate,
        CancellationTokenSource cts) : base(translate)
    {
        token = cts.Token;
        ErrorWarn = new(appConfig, translate, cts);
        token.Register(() => CurrentVisibility = Visibility.Collapsed);
        _ = UpdateExpectedTime();
    }

    /// <summary>
    /// 文件数量
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial int Total { get; set; }

    /// <summary>
    /// 文件完成数量
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial double Current { get; set; }

    /// <summary>
    /// 耗时
    /// </summary>
    [ObservableProperty]
    public partial string TimeConsuming { get; set; } = "";

    [ObservableProperty]
    public partial Visibility CurrentVisibility { get; set; } = Visibility.Visible;

    public double Progress => (Current / Total).UnNaN();

    protected override Control CreateControl() =>
        new FileCountControl() { DataContext = this };

    public ErrorWarnViewModel ErrorWarn { get; set; } = null!;

    // 新增文件
    void IRecipient<IncreaseFile>.Receive(IncreaseFile message)
    {
        lock (@lock)
            Total += message.Count;
    }

    // 完成文件
    void IRecipient<DecreaseFile>.Receive(DecreaseFile message)
    {
        lock (@lock)
            Current += message.Count;
    }

    public void SetEnd()
    {
        isEnd = true;
        CurrentVisibility = Visibility.Collapsed;
    }

    private async Task UpdateExpectedTime()
    {
        while (!token.IsCancellationRequested && !isEnd)
        {
            await Task.Delay(1000);
            TimeConsuming = (DateTime.Now - start).ForamtCropping();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        ErrorWarn?.Dispose();
        GC.SuppressFinalize(this);
    }
}