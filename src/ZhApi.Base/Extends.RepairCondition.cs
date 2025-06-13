using ZhApi.Bases;
using ZhApi.Configs;

namespace ZhApi;
public static class RepairConditionHelper
{
    public static IQueryable<T> WhereRepair<T>(this IQueryable<T> q, RepairCondition? rc)
        where T : KvRowBase
    {
        if (rc is null) return q;

        //  按评分筛选
        q = new RangeData(rc.ScoreRange).WhereRepair(q, x => x.Score);

        // 按修复次数筛选
        q = new RangeData(rc.RepairRange).WhereRepair(q, x => x.RepairCount);

        return q;
    }
}