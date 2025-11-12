using Microsoft.EntityFrameworkCore.Update;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZhApi.Cores;
using ZhApi.Messages;

namespace ZhApi.SqliteDataBase.Imports;
public abstract class ImportBase : ICompletionTask, IDisposable
{
    private readonly ConcurrentDictionary<string, RowInfoPack> idMaps = [];
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

    public async IAsyncEnumerable<RowInfo[]> GetIdsAsync(IQueryable<KvRow> tab)
    {
        var pageIndex = 0;
        while (true)
        {
            if (token.IsCancellationRequested) break;
            var res = await tab.Page(pageIndex, pageSize)
                .Select(x => new RowInfo(x.Id, x.UpdateTime))
                .ToArrayAsync();
            if (res.Length is 0) break;
            yield return res;
            pageIndex++;
        }
    }

    public void SendIds(List<RowInfoPack> ids)
    {
        ids.ForEach(x => idMaps[x.Id] = x);
    }

    public async IAsyncEnumerable<List<KvRowSourcePack>> GetKvRowSources(KvDbContext db)
    {
        var kvTab = db.KvRows.AsNoTracking();
        var sourceTab = db.Sources.AsNoTracking();
        foreach (var item in idMaps.Values.Chunk(pageSize))
        {
            if (token.IsCancellationRequested) yield break;
            var ids = item.Select(x => x.Id).ToHashSet();
            var rows = await GetKvRows(ids, kvTab, sourceTab).ToArrayAsync();
            yield return rows.Select(CreateKvRowSourcePack).ToList();
        }
    }

    private KvRowSourcePack CreateKvRowSourcePack(KvRowSource row)
    {
        var info = idMaps[row.KvRow.Id];
        return new(row, info);
    }

    private static IQueryable<KvRowSource> GetKvRows(HashSet<string> ids,
        IQueryable<KvRow> kvTab, IQueryable<SourceName> sourceTab)
    {
        return from kv in kvTab.Where(x => ids.Contains(x.Id))
               join source in sourceTab
               on Math.Abs(kv.SourceId) equals Math.Abs(source.Id) into g
               from so in g.DefaultIfEmpty()
               select new KvRowSource(kv, so);
    }

    public async Task<string> SaveRowsAsync(List<KvRowSourcePack> rows,
        Dictionary<string, long> sourceMap)
    {
        await UpdateSourceNameAsync(rows, sourceMap);
      return  await SaveRowsAsync(rows);
    }

    private async Task<string> SaveRowsAsync(List<KvRowSourcePack> rows)
    {
        if (rows.Count is 0) return string.Empty;
        using var db = await CreateDbContextAsync();
        var tab = db.KvRows;
        var start = DateTime.Now;

        foreach (var row in rows)
        {
            var item = row.KvRowSource.KvRow;
            switch (row.RowInfoPack.State)
            {
                case EntityState.Modified:
                    tab.Update(item);
                    break;
                case EntityState.Added:
                    tab.Add(item);
                    break;
            }
        }
        var count = await db.SaveChangesAsync();    
        var res =DateTime.Now - start;
        NotifySaveRowCount(count);
        return $"{res.TotalMilliseconds:N0}ms";
    }

    private async Task UpdateSourceNameAsync(List<KvRowSourcePack> row,
        Dictionary<string, long> sourceMap)
    {
        var names = row.Select(x => x.SourceName).ToHashSet()
            .Where(x => !sourceMap.ContainsKey(x)).ToArray();
        if (names.Length is 0) return;

        var ids = sourceMap.Values.ToHashSet();
        var sourceNames = await GetSourceNamesAsync(names, ids);
        sourceNames.ForEach(x => sourceMap[x.Name] = x.Id);
        row.ForEach(x => x.SetSourceName(sourceMap));
    }

    private async Task<List<SourceName>> GetSourceNamesAsync(IEnumerable<string> names,
        HashSet<long> ids)
    {
        using var db = await CreateDbContextAsync();
        var tab = db.Sources;

        foreach (var name in names)
        {
            if (!await tab.AnyAsync(x => x.Name == name))
                await tab.AddAsync(new() { Name = name });
        }

        await db.SaveChangesAsync();
        return await db.Sources.Where(x => !ids.Contains(x.Id)).AsNoTracking().ToListAsync();
    }
}