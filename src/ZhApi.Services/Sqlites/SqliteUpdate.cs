using System.Runtime.CompilerServices;
using ZhApi.Bases;
using ZhApi.MicrosoftAI.Base;

namespace ZhApi.Sqlites;
[AddService<ITranslateService>(ServiceLifetime.Scoped, Keyed = "sqlite:update")]
public class SqliteUpdate : TranslateServiceBase, IFinalService
{
    private readonly IDbContextFactory<KvDbContext> dbFactory;
    private readonly ChannelTimeHandler<IRootData> batch;
    private readonly DateTime dateTime = DateTime.Now;
    private readonly ScopedConfig scopedConfig;
    private readonly AppConfig appConfig;
    private readonly FileLog fileLog;
    private readonly int capacity;

    public SqliteUpdate(IDbContextFactory<KvDbContext> dbFactory,
          IServiceProvider service, IOptionsSnapshot<AppConfig> appConfig,
          IOptionsSnapshot<SqliteConfig> config,
          ScopedConfig scopedConfig, FileLog fileLog,
          CancellationTokenSource cts) : base(service)
    {
        this.appConfig = appConfig.Value;
        this.dbFactory = dbFactory;
        this.scopedConfig = scopedConfig;
        this.fileLog = fileLog;

        var sqlite = config.Value;
        var interval = sqlite.Interval;
        capacity = sqlite.BatchSize;
        batch = new ChannelTimeHandler<IRootData>(SaveHandler, interval, cts.Token);
    }

    public override bool IsShowData { get; set; } = true;

    public override ConfigBase Config { get; } = new()
    {
        Title = "修复",
        Type = "sqlite",
        Model = "sqlite",
        Enable = true,
    };

    public override Task Completion() => batch.Completion();

    protected override Task SendKeyAsync(KeyData key) => KeyHandlerAsync(key);

    protected override async Task KeyHandlerAsync(KeyData key)
    {
        // 有效服务
        var count = await scopedConfig.CountAsync();
        if (count is 0) throw new ConfigException("找不到可用的翻译服务！");

        var data = DataService.GetRootData(key);
        var original = data.Items.FirstOrDefault()!;
        var score = data.GetScore()!;
        var value = new UpdateData();
        var id = data.Tag as string ?? "未知";
        data.Tag = value;

        if (IsNull(data, original, id) || IsNull(data, score, id))
            return;

        await KeyHandlerAsync(key, data, original, score, id, value);

    }

    private async Task KeyHandlerAsync(KeyData key, IRootData data,
        ScoreData original, ScoreData score, string id, UpdateData value)
    {
        if (IsCancellationRequested) return;

        // 修复成功
        if (score.IsPerfect)
        {
            value.IsUpdateSourceId = value.IsUpdateValue = true;
            Log(LogLevel.Information, id, data, "✔️ 修复成功", GetLog());
        }

        // 达到最低修复标准
        else if (appConfig.IsRepairStandards(key))
        {
            value.IsUpdateSourceId = value.IsUpdateValue = true;
            Log(LogLevel.Information, id, data, "达到修复标准", GetLog());
        }

        // 不完整修复
        else if (original.Value > score.Value)
        {
            value.IsUpdateSourceId = value.IsUpdateValue = true;
            Log(LogLevel.Warning, id, data, "🚫 不完整修复", GetLog());
        }

        // 不在配置target的范围中
        else if (!score.IsTarget(Config.Target))
        {
            value.IsUpdateSourceId = true;
            Log(LogLevel.Information, id, data,
                "强制放行！因为不在配置target的范围中。", GetLog());
        }

        // 修复失败
        else
        {
            Log(LogLevel.Error, id, data, "修复失败", GetLog());
        }

        await batch.SendAsync(data);

        string GetLog()
        {
            var oXml = original.Xml;
            var nXml = score.Xml;
            return $"""
            原文: length:{data.OriginalXml.Length}
            {data.OriginalXml.GetXmlMarkdown()}
            原始: v:{original.Value} | e:{original.ErrorSimple}
            {oXml.GetXmlMarkdown()}
            当前: v:{score.Value} | e:{score.ErrorSimple}
            {nXml.GetXmlMarkdown()}
            {data.GetLog()}
            """;
        }
    }

    private bool IsNull(IRootData data, ScoreData? score, string id,
        [CallerArgumentExpression(nameof(score))] string name = "")
    {
        if (score is null)
        {
            Log(LogLevel.Error, id, data, $"{name} is null", "非预期结果");
            return true;
        }
        return false;
    }

    private void Log(LogLevel logLevel, string id, IRootData data,
        string title, string? msg = "")
    {
        switch (logLevel)
        {
            case LogLevel.Information:
                new IncreaseUnitNode(this, 1).SendMessage();
                break;
            case LogLevel.Warning:
                new RequestWarn(this).SendMessage();
                break;
            case LogLevel.Error:
                new RequestError(this).SendMessage();
                break;
            default:
                break;
        }

        fileLog.Log(logLevel, """
        {title}
        id: {id}
        {msg}
        """,
        title,
        id,
        msg ?? string.Empty);
    }

    private static async Task<Dictionary<string, KvRow>> GetIdMapAsync(
      KvDbContext db, IRootData[] datas)
    {
        var ids = datas.Select(x => x.Index).ToHashSet();
        var items = await db.KvRows.Where(x => ids.Contains(x.Id)).ToArrayAsync();
        return items.DistinctBy(x => x.Id).ToDictionary(x => x.Id);
    }

    private async Task SaveHandler(IRootData[] datas)
    {
        if (datas.Length is 0) return;
        var items = datas.Chunk(capacity);
        foreach (var item in items)
            await SaveAsync(item);
    }

    private async Task SaveAsync(IRootData[] datas)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var map = await GetIdMapAsync(db, datas);
        foreach (var item in datas)
        {
            var row = map[item.Index];
            Update(item, row);
        }

        var count = await db.SaveChangesAsync();
        new DataBaseMessage(count, IsUpdate: true).SendMessage();
    }

    private void Update(IRootData item, KvRow row)
    {
        var score = item.GetScore();
        var value = (UpdateData)item.Tag!;
        row.RepairCount += 1;
        if (value.IsUpdateSourceId)
        {
            row.SourceId = GetSourceId(row, score);
            row.UpdateTime = dateTime;
        }

        if (value.IsUpdateValue && score != null && score.Xml != null)
        {
            row.Tag = score.ErrorSimple ?? string.Empty;
            row.Translation = score.Xml;
            row.Score = score.Value;
            row.UpdateTime = dateTime;
        }
    }

    private static long GetSourceId(KvRow kvRow, ScoreData? score)
    {
        var res = Math.Abs(kvRow.SourceId);
        if (score != null && score.IsPerfect) return res;
        return res * -1;
    }

    class UpdateData
    {
        public bool IsUpdateValue { get; set; }
        public bool IsUpdateSourceId { get; set; }

    }
}