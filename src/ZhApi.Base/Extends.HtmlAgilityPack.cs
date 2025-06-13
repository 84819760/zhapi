using HtmlAgilityPack;
namespace ZhApi;

partial class Extends
{
    static void RemoveElementsFlags()
    {
        var map = HtmlNode.ElementsFlags;
        foreach (var item in map) map.Remove(item.Key);
    }

    public static HtmlNode ToHtmlNode(this string xml)
    {
        var doc = xml.ToHtmlDocument();
        return doc.DocumentNode;
    }

    public static HtmlDocument ToHtmlDocument(this string xml)
    {
        var doc = new HtmlDocument()
        {
            OptionEmptyCollection = true,
            OptionTreatCDataBlockAsComment = true
        };
        doc.LoadHtml(xml);
        return doc;
    }

}
