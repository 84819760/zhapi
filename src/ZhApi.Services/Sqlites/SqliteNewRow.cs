using ZhApi.MicrosoftAI.Base;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ZhApi.Sqlites;

[AddService<ITranslateService>(ServiceLifetime.Scoped, Keyed = "sqlite:add")]
public class SqliteNewRow : TranslateServiceBase, IFinalService
{
    private readonly KvRowTimeService kvRowService;
    private readonly SourceNameService sourceName;
    private readonly CancellationTokenSource cts;
    private readonly Lazy<Task<bool>> LazyReady;
    private readonly ScopedConfig scopedConfig;
    private readonly IDataService dataService;
    private readonly RunConfig runConfig;
    private readonly FileLog fileLog;
    private readonly Mickey mickey;

    public SqliteNewRow(KvRowTimeService kvRowService,
        SourceNameService sourceName, RunConfig runConfig,
        ScopedConfig scopedConfig, Mickey mickey,
        CancellationTokenSource cts, FileLog fileLog,
        IDataService dataService, IServiceProvider service) : base(service)
    {
        this.scopedConfig = scopedConfig;
        this.kvRowService = kvRowService;
        this.dataService = dataService;
        this.sourceName = sourceName;
        this.runConfig = runConfig;
        this.fileLog = fileLog;
        this.mickey = mickey;
        this.cts = cts;

        LazyReady = new(CreateReady);
    }

    public override ConfigBase Config { get; } = new()
    {
        Enable = true,
        Model = "sqlite.add",
    };

    protected override async Task SendKeyAsync(KeyData key)
    {
        if (await LazyReady.Value)
        {
            Complete(key.Index);
            return;
        }
        await KeyHandlerAsync(key);
    }

    private async Task<bool> CreateReady()
    {
        // 有效服务
        if (await scopedConfig.CountAsync() > 0) return false;
        if (runConfig.StopModel) return false;
        new ExceptionMessage("翻译服务数量为0", IsAppend: true).SendMessage();
        cts.TryCancel();
        return true;
    }

    protected override async Task KeyHandlerAsync(KeyData key)
    {
        var data = dataService.GetRootData(key);
        var score = data.GetScore();

        if (score is null)
        {
            if (!runConfig.StopModel)
                Log(LogLevel.Critical, data, "非预期错误(score 不应为空)");
        }
        else if (score.IsPerfect)
        {
            Log(LogLevel.Debug, data, "翻译完成");
        }
        else if (score.IsFail)
        {
            Log(LogLevel.Error, data, "翻译失败");
        }
        else
        {
            Log(LogLevel.Warning, data, "不完整翻译");
        }
        await AddKvAsync(data, score);
    }

    private async Task AddKvAsync(IRootData data, ScoreData? score)
    {
        const string invalid = "无效数据";
        var rd = score?.Detail.ResponseData;
        var modelName = rd?.ModelInfo.ModelName ?? invalid;
        var sourceId = await sourceName.GetSourceIdAsync(modelName);
        var kv = new KvRow
        {
            Translation = score?.Xml ?? string.Empty,
            Tag = score?.ErrorSimple ?? invalid,
            Callback = () => Complete(data.Index),
            Score = score?.Value ?? 2000,
            Original = data.OriginalXml,
            SourceId = sourceId,
            Id = data.Index,
        };
        await kvRowService.SendAsync(kv);
    }

    private void Complete(string index) =>
        mickey[index].TrySetResult();

    public override async Task Completion() =>
        await kvRowService.Completion();

    private void Log(LogLevel logLevel, IRootData data, string title)
    {
        MessageBase? m = logLevel switch
        {
            LogLevel.Warning => new RequestWarn(this),
            LogLevel.Error or LogLevel.Critical => new RequestError(this),
            _ => null,
        };

        m?.SendMessage();

        var score = data.GetScore();
        var rd = score?.Detail.ResponseData;
        fileLog.Log(logLevel, """
        {title}   id: {id}
        file    : '{file}'
        member  : '{member}'
        score   : {score}
        请求：  {original}
        响应：  {res}
        {log}
        """,
        title,
        data.Index,
        data.PathInfo.FilePath,
        data.PathInfo.MemberPath,
        $"{score?.Value}, {rd?.RequestId} {score?.ErrorSimple}",
        data.OriginalXml.GetXmlMarkdown(),
        rd?.Response,
        data.GetLog());
    }
}