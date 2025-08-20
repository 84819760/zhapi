using System.Data;
using ZhApi.Cores;
using ZhApi.Messages;

namespace ZhApi.SqliteDataBase.Imports;
public abstract class ImportBase : ICompletionTask, IDisposable
{
    private readonly ConcurrentBag<string> idMaps = [];
    private readonly CancellationToken token;
    private readonly int pageSize;
    private readonly string target;

    public ImportBase(IServiceProvider service)
    {
        token = service.GetRequiredService<CancellationTokenSource>().Token;
        var config = service.GetRequiredService<IOptionsSnapshot<SqliteConfig>>().Value;
        pageSize = config.PageSize;
        target = GetType().Name;
    }

    public int Count { get; private set; }

    public int DiffCount => idMaps.Count;

    public virtual void Dispose() => GC.SuppressFinalize(this);

    public virtual Task Completion() => Task.CompletedTask;

    private void NotifySaveRowCount(int count) =>
        new DataBaseMessage(count, Target: target).SendMessage();

    public abstract IDbContextFactory<KvDbContext> GetDbFactory();

    public Task<KvDbContext> CreateDbContextAsync() => GetDbFactory().CreateDbContextAsync();

    public async Task<int> GetCount()
    {
        using var db = await GetDbFactory().CreateDbContextAsync();
        return await db.KvRows.CountAsync();
    }

    public async Task<ImportBase> InitAsync()
    {
        Count = await GetCount();
        return this;
    }

    public async IAsyncEnumerable<string[]> GetIdsAsync(IQueryable<KvRow> tab)
    {
        using var db = await GetDbFactory().CreateDbContextAsync();
        var query = db.KvRows.AsNoTracking();
        var pageIndex = 0;

        while (true)
        {
            if (token.IsCancellationRequested) break;
            var res = await query.Page(pageIndex, pageSize)
                .Select(x => x.Id).ToArrayAsync();
            if (res.Length is 0) break;
            yield return res;
            pageIndex++;
        }
    }

    public async Task SendIdsAsync(string[] item, IQueryable<KvRow> targetTab)
    {
        var ids = item.ToHashSet();

        var exists = await targetTab
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToHashSetAsync();

        foreach (var id in ids.Except(exists))
            idMaps.Add(id);
    }

    public async IAsyncEnumerable<KvRowSource[]> GetDiffRows(KvDbContext db)
    {
        var kvTab = db.KvRows.AsNoTracking();
        var sourceTab = db.Sources.AsNoTracking();
        var sourceMap = await sourceTab.ToDictionaryAsync(k => k.Id, v => v.Name);
      
        foreach (var item in idMaps.AsEnumerable().Chunk(pageSize))
        {
            if (token.IsCancellationRequested) yield break;
            var ids = item.ToHashSet();
            var q = from kv in kvTab
                    join so in sourceTab
                    on kv.SourceId equals Math.Abs(so.Id) into g
                    from so in g.DefaultIfEmpty()
                    where ids.Contains(kv.Id)
                    select new KvRowSource
                    {
                        KvRow = kv,
                        SourceName = so
                    };
            yield return await q.ToArrayAsync();
        }
    }

    public async Task SendRowsAsync(KvRowSource[] rows, Dictionary<string, long> map)
    {
        await InitSourceMap(rows, map);
        Array.ForEach(rows, x => x.UpdateSourceId(map));
        using var db = await CreateDbContextAsync();
        var count = await db.TryAdds(rows.Select(x => x.KvRow).ToArray());
        NotifySaveRowCount(count);
    }

    private async Task InitSourceMap(KvRowSource[] rows, Dictionary<string, long> map)
    {
        var diffs = rows
            .Select(x => x.GetSourceName())
            .Distinct().Where(x => !map.ContainsKey(x))
            .ToArray();

        if (diffs.Length is 0) return;
        using var db = await CreateDbContextAsync();
        var tab = db.Sources;
        foreach (var row in diffs)
        {
            if (!await tab.AnyAsync(x => x.Name == row))
                await db.Sources.AddAsync(new() { Name = row });
        }

        await db.SaveChangesAsync();

        var sources = await db.Sources.ToArrayAsync();

        foreach (var item in sources)
            map[item.Name] = item.Id;
    }

}

public class KvRowSource
{
    public required KvRow KvRow { get; init; }

    public SourceName? SourceName { get; init; }

    public string GetSourceName() => SourceName?.Name ?? "数据库导入";

    public void UpdateSourceId(Dictionary<string, long> map)
    {
        var sourceName = GetSourceName();
        if (map.TryGetValue(sourceName, out var sourceId))
        {
            KvRow.SourceId = sourceId;
        }
        else
        {
            // 待补充（可能性？）
        }
    }
}