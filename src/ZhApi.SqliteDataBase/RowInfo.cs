using System.Runtime.InteropServices;
using ZhApi.Cores;

namespace ZhApi.SqliteDataBase;
public record class RowInfo(string Id, DateTime UpdateTime);

public class RowInfoPack(RowInfo source)
{
    public RowInfo SourceInfo { get; } = source;

    public RowInfo? TargetInfo { get; set; }

    public string Id => SourceInfo.Id;

    public bool IsOut => State > EntityState.Unchanged;

    public EntityState State
    {
        get
        {
            if (TargetInfo is null)
                return EntityState.Added;

            if (SourceInfo.UpdateTime > TargetInfo.UpdateTime)
                return EntityState.Modified;

            return EntityState.Unchanged;
        }
    }
}

public record KvRowSource(KvRow KvRow, SourceName? SourceName);

public record KvRowSourcePack(KvRowSource KvRowSource, RowInfoPack RowInfoPack)
{
    public string SourceName => KvRowSource.SourceName?.Name ?? "数据库导入";

    public void SetSourceName(Dictionary<string, long> map)
    {
        var sourceId = map[SourceName];
        if (KvRowSource.KvRow.SourceId < 0)
            sourceId = Math.Abs(sourceId) * -1;
        KvRowSource.KvRow.SourceId = sourceId;
    }
}

