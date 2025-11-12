using Microsoft.Data.Sqlite;

namespace ZhApi.SqliteDataBase;
public abstract class DbContextBase : DbContext
{
    private readonly static SemaphoreSlim slim = new(1);
    private readonly string? dbPath;

    static DbContextBase() => AppDomain.CurrentDomain
         .ProcessExit += (_, _) => ClearAllPools();

    public DbContextBase(DbContextOptions options) : base(options)
    {

    }

    public DbContextBase(string dbPath) => this.dbPath = dbPath;

    public SemaphoreSlim SemaphoreSlim { get; set; } = slim;

    public static void ClearAllPools() => SqliteConnection.ClearAllPools();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (dbPath is { Length: > 0 })
        {
            optionsBuilder.UseSqliteParameterized($"Data Source={dbPath}");
        }
        else
        {
            base.OnConfiguring(optionsBuilder);
        }
    }

    public override async Task<int> SaveChangesAsync(
      CancellationToken cancellationToken = default)
    {
        await SemaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            SemaphoreSlim.Release();
        }
    }
}