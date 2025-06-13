using HtmlAgilityPack;
using System.Diagnostics;
using System.Web;
using ZhApi.XmlBuilders;

namespace ZhApi.Nodes;
[DebuggerDisplay("CodeNode :name={Name},index={Index},{CodeLines.Count}")]
public class CodeNode : AttributeNode
{
    public static CodeNode Create(HtmlNode html, PathInfo info, NodeBase parent)
    {
        var res = new CodeNode();
        res.Set(html, info, parent);
        res.SetAttribute(html.Attributes);
        res.CodeLines.AddRange(GetCodeLines(html));
        return res;
    }

    internal override void XmlBuild(XmlBuilder builder, XmlNode parant) =>
        builder.CodeHandler(this, parant);

    public override async Task Write(XmlFileStreamNode stream)
    {
        var codes = HtmlEncode();
        if (CodeLines.Count < 2)
        {
            var code = codes.FirstOrDefault();
            await stream.Write($"<c>{code}</c>");
            return;
        }

        await stream.WriteScope(() => WriteCode(stream, codes));
        await stream.WriteTab();
    }

    private async Task WriteCode(XmlFileStreamNode stream, IEnumerable<string> codes)
    {
        var start = GetNameXmlLine();
        await stream.WriteLine(writeTab: false);
        await stream.WriteLine($"{start}>");

        foreach (var item in codes)
            await stream.WriteLine(item);

        await stream.WriteLine($"</{Name}>");
    }

    private IEnumerable<string> HtmlEncode()
    {
        foreach (var line in CodeLines)
            yield return HttpUtility.HtmlEncode(line);
    }
}
