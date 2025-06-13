using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace ZhApi.Cores;
public readonly struct RangeData
{
    const StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    public RangeData(string? range)
    {
        if (range is null) return;
        foreach (var item in range.Split("..", options).Select(x => x.Trim()))
        {
            if (item.StartsWith('['))
            {
                Start = GetValue(item);
            }
            else if (item.EndsWith(']'))
            {
                End = GetValue(item);
            }
        }
    }

    public int Start { get; }

    public int End { get; }

    public bool IsDefault => Start is 0 && End is 0;

    private static int GetValue(string txt)
    {
        txt = txt.Trim(['[', ']']).Trim();
        _ = int.TryParse(txt, out var res);
        return res;
    }

    public IQueryable<T> WhereRepair<T>(IQueryable<T> q, Expression<Func<T, dynamic>> exp)
    {
        var list = new List<string>(2);
        var propName = GetProperty(exp).Name;
        if (Start > 0) list.Add($"{propName} >= {Start}");
        if (End > 0) list.Add($"{propName} < {End}");
        if (list.Count is 0) return q;
        var w = list.JoinString(" and ");
        return q.Where(w);
    }

    private static MemberInfo GetProperty<T>(Expression<Func<T, dynamic>> exp) =>
        exp.GetProperty();
}