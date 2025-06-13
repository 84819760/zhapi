#pragma warning disable
using ZhApi.SqliteDataBase;

namespace ZhApi;
public static class Extend
{
    public static Task<bool> IsExistAsync(this IQueryable<KvRow> q, KvRow kvRow)
    {
        return IsExistIdAsync(q, kvRow.Id);
    }

    /// <summary>
    /// 检查记录是否存在
    /// </summary>
    public static Task<bool> IsExistIdAsync(this IQueryable<KvRow> q, string id)
    {
        return q.AsNoTracking().AnyAsync(x => x.Id == id);
    }

    /// <summary>
    /// 设置分页
    /// </summary>
    public static IQueryable<T> Page<T>(this IQueryable<T> q, int pageIndex, int pageSize)
    {
        var skip = pageIndex * pageSize;
        return q.Skip(skip).Take(pageSize);
    }
}