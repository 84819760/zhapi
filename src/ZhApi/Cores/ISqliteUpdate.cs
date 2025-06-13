namespace ZhApi.WpfApp.Cores;
public interface ISqliteUpdate
{
    Task SqliteUpdateAsync(SqliteUpdateService sqlite);
}