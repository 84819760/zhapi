using ZhApi.Messages;

namespace ZhApi.SqliteDataBase;
public class KvRowTimeService : IDisposable, ICompletionTask
{
    private readonly IDbContextFactory<KvDbContext> dbFactory;
    private readonly ChannelTimeHandler<KvRow> channel;
    private readonly int capacity;


    [AddService(ServiceLifetime.Scoped)]
    public KvRowTimeService(
        IDbContextFactory<KvDbContext> dbFactory,
        IOptionsSnapshot<SqliteConfig> config,
        CancellationTokenSource cts)
    {
        this.dbFactory = dbFactory;

        var sqliteConfig = config.Value;
        capacity = sqliteConfig.BatchSize;
        channel = new(Handler, sqliteConfig.Interval, cts.Token);
    }

    public Action<int>? NotifySaveRowCount { get; set; }
        = count => new DataBaseMessage(count).SendMessage();

    public async Task SendAsync(KvRow kvRow) =>
        await channel.SendAsync(kvRow);

    private async Task Handler(KvRow[] values)
    {
        var items = values.Chunk(capacity);
        foreach (var item in items)
            await AddAsync(item);
    }

    private async Task AddAsync(KvRow[] values)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        db.NotifySaveRowCount = NotifySaveRowCount;
        await db.TryAdds(values);
    }

    public Task Completion() => channel.Completion();

    public void Dispose()
    {
        channel.Dispose();
        GC.SuppressFinalize(this);
    }

    //public static KvRowTimeService CreateSync(
    //   IDbContextFactory<KvDbContext> dbFactory,
    //   IOptionsSnapshot<SqliteConfig> config,
    //   CancellationTokenSource cts, Action<int> notify) =>
    //   new(dbFactory, config, cts) { NotifySaveRowCount = notify };
}