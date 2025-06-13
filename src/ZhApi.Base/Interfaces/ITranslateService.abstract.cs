#if true1
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZhApi.Configs;
using ZhApi.Messages;
using ZhApi.Packs;

namespace ZhApi.Interfaces;
public abstract class TranslateServiceBase : ITranslateService
{
    private readonly IdProvider idProvider = null!;
    private ITranslateService nextService = null!;
    private readonly Lazy<bool> firstLazy = null!;
    protected readonly CancellationToken token;

    /// <summary>
    /// 是否为首个服务
    /// </summary>
    private bool isFirstService;

    public TranslateServiceBase(CancellationTokenSource cts,
        IdProvider idProvider)
    {
        this.idProvider = idProvider;
        token = cts.Token;
        firstLazy = new(FirstLazy);
    }

    protected TranslateServiceBase() { }

    public virtual bool IsShowData => true;

    public abstract ConfigBase Config { get; }

    public virtual bool IsSingleton { get; }

    public bool IsEnd { get; set; }

    public bool IsOk => Config.IsOk;

    public virtual string ModelName => Config.Model?.Trim()
        ?? throw new Exception($"{Config.Info} ModelName未设置！");

    public virtual Task SendAsync(UnitPackBase unitPack)
    {
        if (!IsOk || unitPack.IsPerfect)
            return next();

        // 是否未目标类型，否则发送到下个节点
        if (!unitPack.IsTarget(Config.Target))
            return next();

        return add();
        Task next() => Next.SendAsync(unitPack);
        Task add()
        {
            if (!isFirstService) new IncreaseUnitNode(this, 1).SendMessage();
            _ = firstLazy.Value;
            return PackHandle(unitPack).WaitAsync(token);
        }
    }

    public virtual Task Start() => Task.CompletedTask;

    // 首次调用
    private bool FirstLazy()
    {
        new TranslateServiceStateMessage(this, TranslateServiceState.First)
            .SendMessage();
        return true;
    }

    protected abstract Task PackHandle(UnitPackBase unitPack);

    protected ITranslateService Next => nextService
        ?? throw new Exception("未设置下一个服务");

    public virtual ITranslateService SendNextService(ITranslateService nextService) =>
        this.nextService = nextService;

    public abstract ITranslateService SendConfig(IConfiguration config, int index);

    public virtual ITranslateService SendCount(int count)
    {
        isFirstService = true;
        new IncreaseUnitNode(this, count).SendMessage();
        return this;
    }

    public virtual Task StopModel() => Task.CompletedTask;

    public virtual Task Init()
    {
        Config.ExceptionMessage ??= Config.GetError();
        return Task.CompletedTask;
    }

    public abstract Task Completion();

    public abstract Task<HashSet<string>> GetModels();

    /// <summary>
    /// 文本方式请求
    /// </summary>
    public abstract Task<ResponseData> GetResponseAsync(string markdown);

    /// <summary>
    /// 对话方式请求
    /// </summary>
    public abstract Task<ResponseData> GetResponseChatAsync(Score score);

    protected Task<ResponseData> GetResponseData(UnitPackBase pack)
    {
        // 第一次请求 或 非对话重试模式
        if (pack.Count is 0 || !Config.GetRetry().Chat)
            return GetResponseAsync(pack.Markdown);

        var score = pack.AllScore[^1];

        // 不是同一个模型不做对话重试
        if (score.Model != ModelName)
            return GetResponseAsync(pack.Markdown);

        // 对话重试模式
        return GetResponseChatAsync(score);
    }

    private async Task<Score?> ResponseAsync(UnitPackBase pack, int index)
    {
        if (token.IsCancellationRequested) return default;
        var retry = Config.Retry ?? new();
        var rd = await GetResponseData(pack);


        if (token.IsCancellationRequested) return default;

        // 报告 耗时      
        new RequestTimeConsuming(this, rd.TimeConsuming,
            rd.Request.Length, rd.Response.Length).SendMessage();

        var score = rd.CreateScore(index, pack, idProvider);

        // 是否为重试目标
        if (!pack.IsTarget(Config.Target)) return score;

        // 是否 重试
        if (!retry.IsRetry(pack, score, index)) return score;

        // 报告 重试
        new RequestRetry(this).SendMessage();
        return await ResponseAsync(pack, index + 1);
    }


    /// <summary>
    /// 调用<see cref="GetResponseAsync(string)"/>,并且将结果发送到下个服务
    /// </summary>
    protected async ValueTask Execute(UnitPackBase pack, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        if (IsOk)
        {
            var score = await ResponseAsync(pack, 0);
            pack.IsRequest = true;
            Report(score, pack);
        }
        if (token.IsCancellationRequested) return;
        new DecreaseUnitNode(this).SendMessage();
        await Next.SendAsync(pack);
    }

    /// <summary>
    /// 报告处理结果，并且设置nextCount
    /// </summary>
    protected void Report(Score? score, UnitPackBase pack)
    {
        if (token.IsCancellationRequested) return;
        if (score is null || score.IsFail)
        {
            new RequestError(this).SendMessage();
            TryLog(pack, "Error: 翻译失败\r\n{msg}");
        }
        else if (!score.IsPerfect)
        {
            new RequestWarn(this).SendMessage();
            TryLog(pack, "Warning: 不完整翻译\r\n{msg}");
        }
    }

    private void TryLog(UnitPackBase unitPack, string msg)
    {
        if (Next is null || Next.IsEnd) return;
        FileLog.Default.LogDebug(msg, unitPack.GetLog());
    }
}
#endif