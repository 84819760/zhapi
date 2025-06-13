using HtmlAgilityPack;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Web;
using ZhApi.XmlBuilders;

namespace ZhApi.Nodes;
[DebuggerDisplay("TextNode : index={Index},text={Text}")]
public partial class TextNode : NodeBase
{
    public string Text { get; set; } = null!;

    [JsonIgnore]
    public bool IsChinese => Text.IsContainChinese();

    public static TextNode Create(HtmlTextNode html, PathInfo info, NodeBase parent)
    {
        // var res = NodePool<TextNode>.Get(html, info, parent);
        var res = new TextNode
        {
            Text = GetText(html),
            Name = nameof(TextNode)
        };
        res.Set(html, info, parent);
        return res;
    }

    private static string GetText(HtmlTextNode html)
    {
        var t = HttpUtility.HtmlDecode(html.Text);
        var v = t.Replace('\r', ' ').Replace('\n', ' ');
        return ReplaceSpace(v);
    }

    public static string ReplaceSpace(string txt)
    {
        while (txt.Contains("  "))
            txt = txt.Replace("  ", " ");

        return txt.Trim();
    }

    internal override void XmlBuild(XmlBuilder builder, XmlNode parant) =>
        builder.TextHandler(this, parant);

    public override void UseAttributeId()
    {
        Index = -1;
    }

    public override Task Write(XmlFileStreamNode stream)
    {
        var txt = HttpUtility.HtmlEncode(Text);
        return stream.Write(txt);
    }
}