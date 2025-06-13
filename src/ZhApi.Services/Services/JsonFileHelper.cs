namespace ZhApi.Services;
[AddService(ServiceLifetime.Scoped)]
public class JsonFileHelper
{
    private const string fileDefault = "zhapi\\jsons\\KvRows.json";
    private readonly IDbContextFactory<KvDbContext> dbFactory;
    private readonly SourceNameService sourceNameService;
    private readonly BatchBase<KvRow> batch;

    public JsonFileHelper(IDbContextFactory<KvDbContext> dbFactory,
        SourceNameService sourceNameService)
    {
        this.sourceNameService = sourceNameService;
        this.dbFactory = dbFactory;
        batch = new BatchAction<KvRow>(3000, Handler);
    }

    public async Task ExportJsonAsync(string file = fileDefault)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var kvRows = db.KvRows.Select(x => new { x.Original, x.Translation })
            .AsAsyncEnumerable();

        using var fs = File.OpenWrite(file);
        await JsonHelper.SerializeEnumerableAsync(fs, kvRows);
        Console.WriteLine($"ExportJson ok");
    }

    public async Task ImportJsonAsync(string file = fileDefault)
    {
        var sourceId = await sourceNameService.GetSourceIdAsync("文件导入");
        using var fs = File.OpenRead(file);

        var items = JsonHelper.DeserializeEnumerableAsync<TranslationData>(fs);
        await foreach (var item in items)
            await SendAsync(item!, sourceId);

        await batch.Completion();
        DbContextBase.ClearAllPools();
        Console.WriteLine("文件导入完成");
    }

    private async Task SendAsync(TranslationData item, long sourceId)
    {
        var score = item.CreateScore();
        if (score is null) return;
        var kvRow = new KvRow()
        {
            Translation = item.Translation,
            Original = item.Original,
            Tag = score.ErrorSimple,
            Score = score.Value,
            SourceId = sourceId,
        };
        await batch.SendAsync(kvRow);
    }

    private async Task Handler(KvRow[] kvRows)
    {
        var t = DateTime.Now;
        using var db = await dbFactory.CreateDbContextAsync();
        var count = await db.TryAdds(kvRows);
        var total = await db.KvRows.CountAsync();
        var diff = (DateTime.Now - t).TotalMilliseconds;
        Console.WriteLine($"写入:{count:N0}, 总数:{total:N0}, 耗时:{diff:N0}ms");
    }
}
