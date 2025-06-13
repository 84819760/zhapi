namespace ZhApi.SqliteDataBase.Imports;
public class ImportRow(KvRow kvRow, string sourceName)
{
    public KvRow KvRow => kvRow;

    public string Id => KvRow.Id;

    public string SourceName => sourceName;

    private static Task<Dictionary<string, KvRow>> GetMapAsync(
        KvDbContext db, ImportRow[] rows)
    {
        var ids = rows.Select(x => x.Id).ToHashSet();
        return db.KvRows.Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
    }

    internal static async Task UpdateAsync(
        KvDbContext db, ImportRow[] items,
        SourceNameService nameService, int maxLength)
    {
        var map = await GetMapAsync(db, items);
        foreach (var item in items)
        {
            if (item.KvRow.Original.Length > maxLength) 
                continue;

            if (map.TryGetValue(item.Id, out var local))
            {
                UpdateAsync(db, item, local);
            }
            else
            {
                var kv = item.KvRow;
                kv.SourceId = await nameService.GetSourceIdAsync(item.SourceName);
                await db.KvRows.AddAsync(kv);
            }
        }
    }

    private static void UpdateAsync(KvDbContext db,
        ImportRow item, KvRow local)
    {
        var v = item.KvRow;
        if (local.UpdateTime >= v.UpdateTime) return;

        local.Translation = v.Translation;
        local.RepairCount = v.RepairCount;
        local.UpdateTime = v.UpdateTime;
        local.SourceId = v.SourceId;
        local.Score = v.Score;
        local.Tag = v.Tag;

        db.KvRows.Update(local);
    }
}