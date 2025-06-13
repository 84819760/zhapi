namespace ZhApi.SqliteDataBase.Imports;
public class ImportSourceDataBase(IServiceProvider service, string path)
    : ImportBase(service), IDbContextFactory<KvDbContext>
{
    protected override IDbContextFactory<KvDbContext> GetDbFactory() => this;

    KvDbContext IDbContextFactory<KvDbContext>.CreateDbContext() =>
        KvDbContext.Create(path, slim);
}
