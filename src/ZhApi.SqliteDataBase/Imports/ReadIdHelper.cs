
namespace ZhApi.SqliteDataBase.Imports;
public class ReadIdHelper
{
    private readonly Dictionary<string, RowInfoPack> map;

    public ReadIdHelper(RowInfo[] items)
    {
        map = new Dictionary<string, RowInfoPack>(items.Length);
        foreach (var item in items)
            map[item.Id] = new(item);
    }

    public async Task<List<RowInfoPack>> GetIdsAsync(IQueryable<KvRow> targetTab)
    {
        await FillTargetAsync(targetTab);
        return map.Values.Where(x => x.IsOut).ToList();
    }

    private async Task FillTargetAsync(IQueryable<KvRow> targetTab)
    {
        var ids = map.Keys.ToHashSet();
        var items = await targetTab
            .Where(x => ids.Contains(x.Id))
            .Select(x => new RowInfo(x.Id, x.UpdateTime))
            .ToArrayAsync();

        foreach (var item in items)
            map[item.Id].TargetInfo = item;
    }
}
