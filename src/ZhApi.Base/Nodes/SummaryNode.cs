using HtmlAgilityPack;
using System.Diagnostics;

namespace ZhApi.Nodes;
[DebuggerDisplay("SummaryNode : name={Name},index={Index},count={Nodes.Count}")]
public class SummaryNode : ElementNode
{
    public static SummaryNode CreateSummary(HtmlNode html, PathInfo info, NodeBase parent)
    {
        info = GetSummaryInfo(html, info);

        //var res = NodePool<SummaryNode>.Get(html, info, parent);
        var res = new SummaryNode();
        res.Set(html, info, parent);
        res.SetAttribute(html.Attributes);

        foreach (var item in html.ChildNodes)
            CreateChildNodes(item, info, res, CreateElement);

        return res;
    }

    private static PathInfo GetSummaryInfo(HtmlNode html, PathInfo info)
    {
        var summaryPath = Extends.StringBuild(sb =>
        {
            sb.Append($"{info.MemberPath} -> {html.Name}");
            if (html.Attributes.Count is 0) return;
            sb.Append('(');
            var items = html.Attributes.Select(x => $"{x.Name}={x.Value}");
            sb.Append(items.JoinString());
            sb.Append(')');
        });

        return info with { MemberPath = summaryPath };
    }

}