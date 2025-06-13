using System.ComponentModel;
using System.Windows.Media.Animation;
using ZhApi.Configs;
using ZhApi.Interfaces;

namespace ZhApi.WpfApp.LogItems;
public partial class ErrorWarnViewModel : ControlProviderMessage,
     IRecipient<IncreaseUnitNode>, IRecipient<DecreaseUnitNode>,
     IRecipient<RequestWarn>, IRecipient<RequestRetry>,
     IRecipient<RequestError>, IRecipient<TranslateServiceStateMessage>,
     IRecipient<RequestTimeConsuming>, IRecipient<RequestLength>,
     IRecipient<ServiceTestMessage>
{
    private readonly TaskCompletionSource completionSource = new();
    private readonly AverageSecond averageSecond = new();
    private readonly CancellationToken token;
    private readonly int parallelism;
    protected bool isEnd;
    private int retry;

    private readonly ITranslateService translate = null!;
    private readonly Task refreshCount = null!;
    private readonly Task taskForTime = null!;

    public static ErrorWarnViewModel Default => new()
    {
        Total = 100,
        Current = 50,
        Error = 10,
        Warn = 10,
        Retry = 10,
        Time = 1.4,
        AverageTime = 2.4,
        Visibility = Visibility.Visible,
    };

    public ErrorWarnViewModel(IOptionsSnapshot<AppConfig> appConfig,
        ITranslateService translate, CancellationTokenSource cts)
        : base(translate)
    {
        this.translate = translate;
        token = cts.Token;

        token.Register(() =>
        {
            RequestItems.Stop();
            TranslateServiceVisibility = Visibility.Collapsed;
            IsActive = false;
        });

        var config = translate.Config;

        parallelism = appConfig.Value.GetParallelism(config);
        ErrorMessage = config.ExceptionMessage;
        IsActive = translate.Config.IsOk;
        Target = GetTarget(translate);

        taskForTime = ForTimeVisibility();
        refreshCount = RefreshCount();

        retry = config.Retry?.Max ?? 1;

        config.PropertyChanged += Config_PropertyChanged;
    }

    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "ExceptionMessage") return;
        ErrorMessage = translate.Config.ExceptionMessage;
    }

    public ErrorWarnViewModel() : this(null!, null!, new()) { }

    [ObservableProperty]
    public partial Visibility Visibility { get; set; } = Visibility.Visible;

    public string Title => translate?.Config.GetName() ?? "未设置标题";

    [ObservableProperty]
    public partial string? TitleTag { get; set; }

    protected override Control CreateControl() =>
        new ErrorWarn() { DataContext = this };

    private static string? GetTarget(ITranslateService translate)
    {
        var t = translate.Config.Target;
        if (translate is IFinalService || t is Configs.Target.All) return default;
        return t.ToString();
    }

    public RequestItemsViewModel RequestItems { get; } = new();

    #region Time
    private readonly ColorAnimation animation = new()
    {
        From = Colors.GreenYellow,
        To = Colors.Gray,
        Duration = TimeSpan.FromSeconds(0.2),
        AutoReverse = false,
    };

    /// <summary>
    /// 耗时
    /// </summary>
    [ObservableProperty]
    public partial double Time { get; set; }

    partial void OnTimeChanged(double value) => this.AppBeginInvoke(() =>
    {
        TimeVisibility = Visibility.Visible;
        TimeBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    });

    public SolidColorBrush TimeBrush { get; set; } = new(Colors.Gray);

    [ObservableProperty]
    public partial double AverageTime { get; set; }

    /// <summary>
    /// 预期完成时间
    /// </summary>
    [ObservableProperty]
    public partial string EstimatedTime { get; set; } = "";

    /// <summary>
    /// 累计耗时
    /// </summary>
    [ObservableProperty]
    public partial string? TimeConsuming { get; set; }

    [ObservableProperty]
    public partial Visibility TimeVisibility { get; set; } = Visibility.Collapsed;

    private async Task ForTimeVisibility()
    {
        await completionSource.Task;
        var start = DateTime.Now;
        while (!token.IsCancellationRequested && !isEnd)
        {
            TimeConsuming = $"{(DateTime.Now - start).Foramt()}";
            await Task.Delay(1000, token);
        }
    }

    #endregion


    #region 服务初始化

    private bool IsOk => translate.Config.IsOk;

    [ObservableProperty]
    public partial string? Target { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    [NotifyPropertyChangedFor(nameof(Opacity))]
    public partial string? ErrorMessage { get; set; }

    /// <summary>
    /// 服务未启用时 显示警告
    /// </summary>
    public Visibility EnableVisibility => translate.Config.Enable
        ? Visibility.Collapsed : Visibility.Visible;

    public double Opacity => IsOk ? 1 : 0.5;

    #endregion

    #region Total Current
    /// <summary>
    /// 要处理总数
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(Visibility))]
    public partial int Total { get; set; }

    /// <summary>
    /// 已处理
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentVisibility))]
    [NotifyPropertyChangedFor(nameof(RetryRate))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(Visibility))]
    public partial int Current { get; set; }

    public double Progress => Current is 0 ? 0 : (double)Current / Total;

    public Visibility CurrentVisibility => Current == Total
       ? Visibility.Collapsed : Visibility.Visible;

    #endregion

    #region Retry  

    /// <summary>
    /// 重试
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RetryRate))]
    [NotifyPropertyChangedFor(nameof(RetryVisibility))]
    [NotifyPropertyChangedFor(nameof(WarnErrorVisibility))]
    public partial int Retry { get; set; }

    /// <summary>
    /// 重试率
    /// </summary>
    public double RetryRate => ((double)Retry / Total).UnNaN();

    public Visibility RetryVisibility => Retry > 0
     ? Visibility.Visible : Visibility.Collapsed;

    #endregion

    #region Warn
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WarnErrorVisibility))]
    [NotifyPropertyChangedFor(nameof(WarnVisibility))]
    [NotifyPropertyChangedFor(nameof(WarnErrorRate))]
    public partial int Warn { get; set; }

    public Visibility WarnVisibility => Warn > 0
        ? Visibility.Visible : Visibility.Collapsed;

    #endregion

    #region Error

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WarnErrorVisibility))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    [NotifyPropertyChangedFor(nameof(WarnErrorRate))]
    public partial int Error { get; set; }

    public Visibility ErrorVisibility => Error > 0
        ? Visibility.Visible : Visibility.Collapsed;

    #endregion

    #region WarnErrorRetry
    public double WarnErrorRate => (((double)Warn + Error) / Total).UnNaN();

    public Visibility WarnErrorVisibility => Warn > 0 || Error > 0 || Retry > 0
      ? Visibility.Visible : Visibility.Collapsed;

    #endregion

    #region IRecipient   
    private int total;
    private int current;

    private async Task RefreshCount()
    {
        while (!token.IsCancellationRequested && !isEnd && translate.IsOk)
        {
            SetTotalCurrent();
            await Task.Delay(500);
        }

        if (!translate.IsOk)
        {
            Total = 0;
            Current = 0;
        }
    }

    private void SetTotalCurrent()
    {
        Total = total;
        Current = current;
    }

    // 增加待处理数量
    void IRecipient<IncreaseUnitNode>.Receive(IncreaseUnitNode message)
    {
        if (message.Target != translate) return;
        Interlocked.Add(ref total, message.Count);

        if (translate is IFinalService)
            SetTotalCurrent();
    }

    // 翻译完成+1
    void IRecipient<DecreaseUnitNode>.Receive(DecreaseUnitNode message)
    {
        if (message.Target != translate) return;
        Interlocked.Add(ref current, 1);
    }

    // 错误+1
    void IRecipient<RequestError>.Receive(RequestError message) =>
       Lock(message, x => Error += 1);

    // 异常+1
    void IRecipient<RequestWarn>.Receive(RequestWarn message) =>
         Lock(message, x => Warn += 1);

    // 重试+1
    void IRecipient<RequestRetry>.Receive(RequestRetry message) =>
         Lock(message, x => Retry += 1);

    #endregion

    #region 翻译服务状态
    void IRecipient<TranslateServiceStateMessage>
    .Receive(TranslateServiceStateMessage message) =>
    Lock(message, x =>
    {
        switch (message.State)
        {
            // 服务启动
            case TranslateServiceState.Start:
                TranslateServiceVisibility = Visibility.Visible;
                TitleTag = ">";
                break;

            // 服务就绪
            case TranslateServiceState.Ready:
                TranslateServiceVisibility = Visibility.Collapsed;
                RequestItems.Visibility = Visibility.Visible;
                completionSource.TrySetResult();
                break;

            // 服务结束
            case TranslateServiceState.End:
                TranslateServiceVisibility =
                TimeVisibility =
                Visibility.Collapsed;
                RequestItems.Stop();
                isEnd = true;
                SetTotalCurrent();
                break;
        }
    });

    void IRecipient<ServiceTestMessage>.Receive(ServiceTestMessage message) =>
    Lock(message, x => TitleTag = message.Title);



    /// <summary>
    /// 翻译服务状态(用于显示启动服务的初始化过程)
    /// </summary>
    [ObservableProperty]
    public partial Visibility TranslateServiceVisibility { get; set; } = Visibility.Collapsed;
    #endregion

    #region RequestTimeConsuming Items 

    [ObservableProperty]
    public partial string? RequestToolTip { get; set; }

    // 请求长度
    void IRecipient<RequestLength>.Receive(RequestLength message) =>
    Lock(message, x =>
    {
        RequestItems.Visibility = Visibility.Visible;
        RequestItems.Add(message);
    });

    void IRecipient<RequestTimeConsuming>.Receive(RequestTimeConsuming message) =>
    Lock(message, x =>
    {
        TranslateServiceVisibility = Visibility.Collapsed;
        RequestItems.Visibility = Visibility.Visible;
        TitleTag = ">";

        var time = message.TimeConsuming;
        AverageTime = averageSecond.GetValue(time);
        Time = time.TotalSeconds;
        RequestItems.Remove(message.Gid);

        SetRequestData(message);
        SetEstimatedTime(retry);
    });

    private void SetEstimatedTime(int timeRetry)
    {
        if (averageSecond.Count < 10) return;
        var p = parallelism <= 0 ? 1 : parallelism;
        var seconds = (Total + Retry) * AverageTime / p;
        EstimatedTime = TimeSpan.FromSeconds(seconds * timeRetry).Foramt();
    }

    void SetRequestData(RequestTimeConsuming message)
    {
        var req = message.RequestLength;
        var res = message.ResponseLength;
        RequestToolTip = $"请求长度:{req:N0}，响应长度:{res:N0}，{message.TimeConsuming.TotalSeconds:N2}";
    }
    #endregion

    public void SendLog()
    {
        if (!translate.Config.IsOk) return;

        var msg = $"{translate.Config.Info} \r\n'总数: {Total:N0}, 重试: {Retry:N0} {RetryRate:P2}, (异常: {Warn:N0}, 错误: {Error}){WarnErrorRate:P1}, 平均: {AverageTime:N2}s, 耗时: {TimeConsuming}";

        FileLog.LogInformation(msg);

    }

    public override Task EndHandlerAsync(Exception? ex = null)
    {
        SendLog();
        (translate as IStopModel)?.StopModel();
        return base.EndHandlerAsync();
    }

    public override void Dispose()
    {
        base.Dispose();
        translate.Config.PropertyChanged -= Config_PropertyChanged;
        taskForTime?.TryDispose();
        refreshCount?.TryDispose();
        GC.SuppressFinalize(this);
    }
}