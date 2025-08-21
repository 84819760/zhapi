using ZhApi.Cores;

namespace ZhApi.SqliteDataBase;
public class KvDbContext : DbContextBase, ITryAdds<KvRow>
{
    public KvDbContext(DbContextOptions options) : base(options) { }

    private KvDbContext(string dbPath) : base(dbPath) { }

    public DbSet<KvRow> KvRows { get; init; } = null!;

    public DbSet<SourceName> Sources { get; init; } = null!;

    public DbSet<DataBaseVersion> Versions { get; init; } = null!;

    public static KvDbContext Create(string dbPath) => Create(dbPath, new(1));

    public static KvDbContext Create(string dbPath, SemaphoreSlim slim) =>
        new(dbPath) { SemaphoreSlim = slim };

    /// <summary>
    /// 保存通知
    /// </summary>
    public Action<int>? NotifySaveRowCount { get; set; }

    /// <summary>
    /// 非重复添加
    /// </summary>
    public async Task<int> TryAdds(KvRow[] values)
    {
        var items = DistinctByOriginal(values);
        var indexs = await FindIndexsAsync(items);

        foreach (var item in items)
        {
            if (!indexs.Contains(item.Id))
                await AddAsync(item);
        }

        var count = await SaveChangesAsync();

        if (count > 0)
            NotifySaveRowCount?.Invoke(count);

        SetEnd(values);

        return count;
    }

    public Task<HashSet<string>> FindIndexsAsync(IEnumerable<KvRow> values)
    {
        var indexs = values.Select(x => x.Id).ToHashSet();
        return KvRows.AsNoTracking()
            .Where(x => indexs.Contains(x.Id))
            .Select(x => x.Id).ToHashSetAsync();
    }

    private static KvRow[] DistinctByOriginal(KvRow[] values) => values
        .Where(x => x.IsOk)
        .DistinctBy(x => x.Id)
        .ToArray();

    private static void SetEnd(KvRow[] values)
    {
        foreach (var item in values)
            item.Callback?.Invoke();
    }
}
