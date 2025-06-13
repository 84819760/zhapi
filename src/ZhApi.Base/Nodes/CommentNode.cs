using HtmlAgilityPack;
using System.Diagnostics;
using ZhApi.XmlBuilders;

namespace ZhApi.Nodes;
[DebuggerDisplay("CommentNode :name={Name},index={Index},{CodeLines.Count}")]
public class CommentNode : AttributeNode
{
    internal static NodeBase Create(HtmlCommentNode comm, PathInfo info, ElementNode parent)
    {
        var res = new CommentNode();
        res.Set(comm, info, parent);
        var txt = GetContent(comm.OuterHtml);
        res.CodeLines.AddRange(GetTabLines(txt));
        return res;
    }

    private static string GetContent(string value)
    {
        const string s = "<![CDATA[";
        var start = value.IndexOf(s);
        if (start < 0) return value;
        var end = value.LastIndexOf("]]>");
        if (end < 0) return value;
        return value[(start + s.Length)..end];
    }

    internal override void XmlBuild(XmlBuilder builder, XmlNode parant)
    {
        builder.CommentHandler(this, parant);
    }

    public override async Task Write(XmlFileStreamNode stream)
    {
        await stream.WriteLine(writeTab: false);
        await stream.WriteLine("<![CDATA[", false);

        foreach (var item in CodeLines)
            await stream.WriteLine(item, false);

        await stream.WriteLine("]]>", false);
    }
}
