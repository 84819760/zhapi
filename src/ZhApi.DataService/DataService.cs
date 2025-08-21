using System.Collections.Concurrent;
using ZhApi.Cores;

namespace ZhApi.DataService;
[AddService<IDataService>(ServiceLifetime.Scoped)]
internal class DataService : IDataService
{
    private readonly ConcurrentDictionary<string, ObjectId> idMap = [];
    private readonly static SemaphoreSlim slim = new(1);
    private readonly ILiteCollection<RootData> list;
    private readonly LiteDatabase liteDatabase;

    public DataService()
    {
        liteDatabase = new(CreateConnection());
        list = liteDatabase.GetCollection<RootData>();
    }

    private static ConnectionString CreateConnection()
    {
        if (!Directory.Exists("zhapi"))
            Directory.CreateDirectory("zhapi");

        var file = Path.Combine("zhapi", "tmp.db");

        if (File.Exists(file))
            File.Delete(file);

        return new()
        {
            Filename = file,
            Connection = ConnectionType.Shared,
            InitialSize = 1024 * 1024 * 50 
        };
    }

    private static async Task WriteAsync(Action action)
    {
        await slim.WaitAsync();
        try
        {
            action();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            slim.Release();
        }
    }

    public Task InsertBulkAsync(IEnumerable<IRootData> datas)
    {
        var items = datas.Select(GetDsRootData);
        return WriteAsync(() => list.InsertBulk(items));
    }

    public Task InsertAsync(IRootData data)
    {
        var item = GetDsRootData(data);
        return WriteAsync(() => list.Insert(item));
    }

    public ObjectId this[string index] =>
        idMap.GetOrAdd(index, ObjectId.NewObjectId());

    private RootData GetDsRootData(IRootData data)
    {
        if (data is RootData rd) return rd;
        return new RootData
        {
            Id = this[data.Index],
            Index = data.Index,
            OriginalXml = data.OriginalXml,
            PathInfo = data.PathInfo,
            Items = data.Items,
            Tag = data.Tag
        };
    }

    public async Task UpdateAsync(IRootData data)
    {
        if (data is RootData rd)
            await WriteAsync(() => list.Update(rd));
    }

    public IRootData GetRootData(KeyData keyData)
    {
        var id = this[keyData.Index];
        return list.FindById(id);
    }

    public void Dispose()
    {
        try
        {
            liteDatabase.Dispose();
        }
        catch (Exception)
        {
        }
        GC.SuppressFinalize(this);
    }
}
