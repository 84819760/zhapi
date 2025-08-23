using Microsoft.Extensions.Configuration;

namespace ZhApi.SqliteDataBase;
partial class ShadowCodeInjectionExtensions
{
    static partial void UseStart(IServiceCollection service, IConfigurationManager? config)
    {
        ArgumentNullException.ThrowIfNull(config);
        const string path = "sqlite:connectionString";
        var conn = config.GetSection(path).Value ?? SqliteConfig.Default;

        service.AddDbContext<KvDbContext>(UseSqlite);
        service.AddPooledDbContextFactory<KvDbContext>(UseSqlite);

        void UseSqlite(DbContextOptionsBuilder builder) =>
            builder.UseSqliteParameterized(conn);
    }
}