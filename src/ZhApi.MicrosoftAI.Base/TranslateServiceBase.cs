using Microsoft.Extensions.Options;

namespace ZhApi.MicrosoftAI.Base;
public abstract class TranslateServiceBase(IServiceProvider service) : ITranslateService
{
    private readonly RunConfig runConfig = service.GetRequiredService<RunConfig>();

    public virtual bool IsShowData { get; set; }

    public bool IsOk => Config.IsOk;

    public abstract ConfigBase Config { get; }

    public IServiceProvider Service { get; } = service;

    public bool IsCancellationRequested => Token.IsCancellationRequested;

    public CancellationToken Token { get; } = service.GetRequiredService<CancellationTokenSource>().Token;

    public AppConfig AppConfig { get; } = service.GetRequiredService<IOptionsSnapshot<AppConfig>>().Value;

    public IDataService DataService { get; } = service.GetRequiredService<IDataService>();

    public ITranslateService Next { get; private set; } = null!;

    public int ServiceIndex
    {
        get => field; private set
        {
            Config.Index = field = value;
        }
    }

    public ITranslateService SetNext(ITranslateService next) => Next = next;

    public virtual ITranslateService SendConfig(IConfiguration config, int index)
    {
        ServiceIndex = index;

        if (runConfig.StopModel)
        {
            Config.ExceptionMessage = new[]
            { Config.ExceptionMessage, "停用模型" }.OfType<string>().JoinString();
        }
        return this;
    }

    public virtual Task InitAsync() => Task.CompletedTask;

    /// <summary>
    /// 用于启动服务(首个服务由程序启动，其它由三个服务启动)
    /// </summary>
    public virtual void Start() => Next?.Start();

    public virtual async Task Completion()
    {
        new TranslateServiceStateMessage(this, TranslateServiceState.End)
            .SendMessage();

        if (Next is null) return;
        Next.Start();
        await Next.Completion();
    }

    protected ParallelOptions CreateParallelOptions() => new()
    {
        CancellationToken = Token,
        MaxDegreeOfParallelism = AppConfig.GetParallelism(Config),
    };

    /// <summary>
    /// 调用者需要实现<see cref="RequestAsync(KeyData, ScoreData?, int)"/>
    /// </summary>
    protected async Task Execute(IRootData data)
    {
        Token.ThrowIfCancellationRequested();
        await RequestAsync(data);
    }

    protected virtual Task RequestAsync(IRootData data, int index = 0) => Task.CompletedTask;

    public async Task SendAsync(KeyData key)
    {
        if (IsCancellationRequested) return;
        await SendKeyAsync(key);
    }

    protected virtual async Task SendKeyAsync(KeyData key)
    {
        // 达到修复标准(发送到下一个)
        if (AppConfig.IsRepairStandards(key))
        {
            await Next.SendAsync(key);
        }
        // 是否为目标
        else if (Config.IsOk && key.IsTarget(Config.Target))
        {
            new IncreaseUnitNode(this, 1).SendMessage();
            await KeyHandlerAsync(key);
        }
        // 否则发送到下一个
        else if (Next != null)
        {
            await Next.SendAsync(key);
        }
    }

    protected abstract Task KeyHandlerAsync(KeyData key);
}
