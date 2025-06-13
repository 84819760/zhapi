namespace ZhApi.Services;
public class SqliteUpdateService(IDbContextFactory<KvDbContext> dbFactory,
    IOptionsSnapshot<AppConfig> appConfig, IDataService dataService,
    CancellationTokenSource cts, TranslateServiceInfo serviceInfo)
{
    private readonly ITranslateService service = serviceInfo.Services[0];
    private readonly ModelInfo modelInfo = new("数据库", "数据库");
    private readonly PathInfo snippetInfo = new("数据库", "");
    private readonly AppConfig appConfig = appConfig.Value;
    private readonly CancellationToken token = cts.Token;
    private readonly int pageSize = 1024;

    public static IQueryable<KvRow> Queryable(KvDbContext db, RepairCondition? rc) =>
        db.KvRows.AsNoTracking().Where(x => x.SourceId > 0 && x.Score > 0).WhereRepair(rc);

    public async Task RunAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var query = Queryable(db, appConfig.RepairCondition);
        var count = await query.CountAsync();
        var pageIndex = 0;

        while (true)
        {
            if (token.IsCancellationRequested) break;
            var res = await query.Page(pageIndex, pageSize).ToArrayAsync();
            if (res.Length is 0) break;
            await SendAsync(res);
            pageIndex++;
        }

        await service.Completion();
    }

    private IRootData CreateOriginal(KvRow kvRow)
    {
        var info = snippetInfo with { MemberPath = $"id:{kvRow.Id}" };
        var data = new RootData()
        {
            OriginalXml = kvRow.Original,
            Index = kvRow.Id,
            PathInfo = info,
            Tag = kvRow.Id,
        };

        data.CreateScore(modelInfo, kvRow.Translation);
        return data;
    }

    private async Task SendAsync(KvRow[] res)
    {
        var items = res.Select(CreateOriginal).ToArray();
        await dataService.InsertBulkAsync(items);
        foreach (var item in items)
        {
            if (token.IsCancellationRequested) return;
            var key = item.GetKey();
            await service.SendAsync(key);
        }
    }
}
