namespace ZhApi.MicrosoftAI.Base;

/// <summary>
/// 无限列队(用于单例服务)
/// </summary>
public abstract class TranslateServiceChannel :
    TranslateServiceBase, IDisposable, IEffectiveTesting
{
    protected readonly Channel<KeyData> channel = Channel.CreateUnbounded<KeyData>();
    private RetryStrategy? retryStrategy;
    private ConfigBase config = null!;
    protected Lazy<Task> startTask;

    public TranslateServiceChannel(IServiceProvider service)
        : base(service) => startTask = new(Run);

    public override ConfigBase Config => config;

    public override bool IsShowData { get; set; } = true;

    protected virtual Task Ready { get; } = Task.CompletedTask;

    protected abstract ChatBase Chat { get; }

    protected override async Task KeyHandlerAsync(KeyData key) =>
        await channel.Writer.WriteAsync(key);

    protected RetryStrategy Retry => retryStrategy ??= (Config.Retry ?? new());

    public override ITranslateService SendConfig(IConfiguration config, int index)
    {
        this.config = GetConfig(config);
        return base.SendConfig(config, index);
    }

    protected abstract ConfigBase GetConfig(IConfiguration configuration);

    public override void Start() => _ = startTask.Value;

    private async Task Run()
    {
        if (IsOk)
        {
            Debug.WriteLine($"开始执行 : {Config.Info}");
            FileLog.Default.LogInformation("""
            启动翻译服务
            序号: {index}
            配置: {json}
            """,
            ServiceIndex,
            Config.GetLog().SerializeLog());
        }

        var options = CreateParallelOptions();
        var items = channel.Reader.ReadAllAsync(Token);
        await Parallel.ForEachAsync(items, options, Execute);
    }

    public override async Task Completion()
    {
        channel.Writer.TryComplete();
        await startTask.Value;
        await base.Completion();
    }

    public void Dispose()
    {
        channel.Writer.TryComplete();
        var __ = channel.Reader.ReadAllAsync().ToBlockingEnumerable().ToArray();
        GC.SuppressFinalize(this);
    }

    private async ValueTask Execute(KeyData key, CancellationToken token)
    {
        Token.ThrowIfCancellationRequested();
        await Ready;
        if (!config.IsOk)
        {
            Next.Start();
            await Next.SendAsync(key);
        }
        else
        {
            var root = DataService.GetRootData(key);
            await Execute(root);
            await DataService.UpdateAsync(root);
            ReportingResults(root);
            var kd = root.GetKey();
            await Next.SendAsync(kd);
        }
        new DecreaseUnitNode(this).SendMessage();
    }

    private void ReportingResults(IRootData data)
    {
        var score = data.GetScore();
        if (score is null || score.IsPerfect) return;

        if (score.IsFail) new RequestError(this).SendMessage();
        else new RequestWarn(this).SendMessage();
    }

    protected override async Task RequestAsync(IRootData data, int index = 0)
    {
        if (IsCancellationRequested)
            return;

        var res = await Chat.RequestAsync(data, index);
        res.CreateScore(data);

        if (index > 0)
            new RequestRetry(this).SendMessage();

        var score = data.GetScore();

        if (IsExit(data, score, index))
            return;

        await RequestAsync(data, index + 1);
    }

    private bool IsExit(IRootData data, ScoreData? score, int index)
    {
        // 超时不重试
        if (data.IsTimeout())
            return true;

        if (score is null)
            return false;

        // 达到修复标准
        if (AppConfig.IsRepairStandards(score.Key))
            return true;

        // 重试策略       
        if (!Retry.IsRetry(data, score, index, Config.Info))
            return true;

        // 重试目标
        if (!score.IsTarget(Config.Target))
            return true;

        return false;
    }

    public async Task<bool> TestAsync()
    {
        if (Chat is IEffectiveTesting t)
            return await t.TestAsync();

        return true;
    }
}