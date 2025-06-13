using ZhApi.Messages;

namespace ZhApi.SqliteDataBase;
[AddService(ServiceLifetime.Scoped)]
public class KvRowService(IDbContextFactory<KvDbContext> dbFactory,
    IOptionsSnapshot<SqliteConfig> config,
    CancellationTokenSource cts)
    : BatchBase<KvRow>(config.Value.BatchSize, token: cts.Token)
{

    protected override async Task Handler(KvRow[] values)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        db.NotifySaveRowCount = NotifySaveRowCount;
        await db.TryAdds(values);
    }

    public Action<int>? NotifySaveRowCount { get; set; }
        = count => new DataBaseMessage(count).SendMessage();


    public static KvRowService CreateSync(
       IDbContextFactory<KvDbContext> dbFactory,
       IOptionsSnapshot<SqliteConfig> config,
       CancellationTokenSource cts, Action<int> notify) =>
       new(dbFactory, config, cts) { NotifySaveRowCount = notify };
}