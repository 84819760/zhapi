
using ZhApi.Messages;

namespace ZhApi.SqliteDataBase.Imports;
public abstract class ImportBase : ICompletionTask, IDisposable
{
    protected readonly SemaphoreSlim slim = new(1);

    private readonly BatchAction<ImportRow> batch;
    private readonly CancellationToken token;
    private readonly string target;
    private readonly int pageSize;
    private readonly int maxLength;

    public ImportBase(IServiceProvider service)
    {
        token = service.GetRequiredService<CancellationTokenSource>().Token;
        maxLength = service.GetRequiredService<IOptionsSnapshot<AppConfig>>().Value.MaxLength ?? 5000;

        var config = service.GetRequiredService<IOptionsSnapshot<SqliteConfig>>().Value;
        pageSize = config.PageSize;
        var batchSize = config.BatchSize;
        batch = new(batchSize, Handler, token: token);
        target = GetType().Name;
    }

    public int Count { get; private set; }

    protected SourceNameService NameService { get; private set; } = null!;

    protected Dictionary<long, string> IdMap { get; private set; } = [];

    private void NotifySaveRowCount(int count) =>
        new DataBaseMessage(count, Target: target).SendMessage();

    protected abstract IDbContextFactory<KvDbContext> GetDbFactory();

    protected async Task<KvDbContext> CreateDbContextAsync()
    {
        var res = await GetDbFactory().CreateDbContextAsync();
        res.SemaphoreSlim = slim;
        return res;
    }

    public async Task<ImportBase> InitAsync()
    {
        NameService = new(GetDbFactory()) { SemaphoreSlim = slim };
        IdMap = await NameService.GetIdMap();

        using var db = await CreateDbContextAsync();
        var tab = db.KvRows.AsNoTracking();

        if (await db.KvRows.AnyAsync())
            Count = await tab.CountAsync();

        return this;
    }

    public virtual Task Completion() => batch.Completion();

    public async Task SendAsync(ImportRow[] rows)
    {
        foreach (var item in rows)
            await batch.SendAsync(item);
    }

    public async IAsyncEnumerable<ImportRow[]> GetRowsAsync()
    {
        if (Count is 0) yield break;
        var db = await CreateDbContextAsync();
        var query = db.KvRows.AsNoTracking();

        var lastTime = await query.MaxAsync(x => x.UpdateTime);
        var pageIndex = 0;

        while (true)
        {
            if (token.IsCancellationRequested) break;
            var res = await GetRowsAsync(query, lastTime, pageIndex);
            if (res.Length is 0) break;
            yield return res;
            pageIndex++;
        }
        await db.DisposeAsync();
    }

    private async Task<ImportRow[]> GetRowsAsync(IQueryable<KvRow> q,
        DateTime lastTime, int pageIndex)
    {
        var res = await q
            .Where(x => x.UpdateTime <= lastTime)
            .Page(pageIndex, pageSize)
            .ToArrayAsync();

        return res.Select(CreateImportRow)
            .OfType<ImportRow>()
            .ToArray();
    }

    private ImportRow? CreateImportRow(KvRow kvRow)
    {
        if (!IdMap.TryGetValue(kvRow.SourceId, out var name)) return null;
        return new ImportRow(kvRow, name);
    }

    public virtual void Dispose() => GC.SuppressFinalize(this);

    private async Task Handler(ImportRow[] rows)
    {
        using var db = await CreateDbContextAsync();
        var items = rows.DistinctBy(x => x.Id).ToArray();
        await ImportRow.UpdateAsync(db, items, NameService, maxLength);
        var count = await db.SaveChangesAsync();
        NotifySaveRowCount(count);
    }
}