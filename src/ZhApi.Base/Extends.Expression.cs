using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ZhApi;
public static class ExpressionHeler
{
    public static MemberInfo GetProperty<T>(this Expression<Func<T, dynamic>> exp)
    {
        var body = exp.Body;
        return GetProperty(body);
    }

    public static MemberInfo GetProperty(this Expression exp)
    {
        return exp.NodeType switch
        {
            ExpressionType.Convert => ((UnaryExpression)exp).Operand.GetProperty(),
            ExpressionType.MemberAccess => ((MemberExpression)exp).Member,
            _ => throw new NotImplementedException($"待补充:{exp.NodeType}"),
        };
    }
}
