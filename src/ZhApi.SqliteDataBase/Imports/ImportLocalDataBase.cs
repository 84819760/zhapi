namespace ZhApi.SqliteDataBase.Imports;

[AddService(ServiceLifetime.Scoped)]
public class ImportLocalDataBase(IServiceProvider service) : ImportBase(service)
{
    private readonly IDbContextFactory<KvDbContext> dbFactory = service.GetRequiredService<IDbContextFactory<KvDbContext>>();

    protected override IDbContextFactory<KvDbContext> GetDbFactory() => dbFactory;
}
