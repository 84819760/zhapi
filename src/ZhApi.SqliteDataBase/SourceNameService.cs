namespace ZhApi.SqliteDataBase;

[AddService(ServiceLifetime.Singleton)]
public class SourceNameService
{
    private readonly ConcurrentDictionary<string, long> sourceMap = [];
    private readonly IDbContextFactory<KvDbContext> dbFactory;
    private readonly static SemaphoreSlim slim = new(1);
    private readonly Lazy<Task> ready;

    public SourceNameService(IDbContextFactory<KvDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
        ready = new(CreateReady);
    }

    private async Task CreateReady()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public SemaphoreSlim SemaphoreSlim { get; set; } = slim;

    public Task<long> GetSourceIdAsync(string name) =>
    SemaphoreSlim.GetTaskAsync(async () =>
    {
        await ready.Value;
        name = name.Trim();

        if (sourceMap.TryGetValue(name, out long id)) return id;

        using var db = await dbFactory.CreateDbContextAsync();
        var tab = db.Sources;
        var row = await tab.FirstOrDefaultAsync(x => x.Name == name);

        if (row is null)
        {
            row = new() { Name = name };
            await tab.AddAsync(row);
            await db.SaveChangesAsync();
        }

        return sourceMap[name] = row?.Id ?? 0;
    });

    public async Task<Dictionary<long, string>> GetIdMap()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.Sources.ToDictionaryAsync(x => x.Id, v => v.Name);
    }
}