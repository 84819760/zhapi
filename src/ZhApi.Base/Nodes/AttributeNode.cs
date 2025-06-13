using HtmlAgilityPack;
using System.Text.Json.Serialization;
using System.Web;

namespace ZhApi.Nodes;
public abstract class AttributeNode : NodeBase
{
    private readonly Dictionary<string, string> attributes =
        new(IgnoreCaseEqualityComparer.Instance);

    public List<string> CodeLines { get; set; } = [];

    [JsonIgnore]
    public bool IsSimple => Name.Equals("c", StringComparison.CurrentCultureIgnoreCase);

    [JsonIgnore]
    public bool IsCodeNode => Name.ToLower() is "code" or "c";

    public void SetAttribute(HtmlAttributeCollection attributes)
    {
        foreach (var item in attributes)
            SetAttribute(item.Name, item.Value);
    }

    public override void SetAttribute(string name, string value)
    {
        this[name] = value;
    }

    public string this[string key]
    {
        get => attributes[key.Trim()];
        set => attributes[key.Trim()] = value.Trim();
    }

    [JsonIgnore]
    public IEnumerable<KeyValuePair<string, string>> Attributes =>
        attributes.Where(x => x.Key != "zhapi_id");

    public bool TryGet(string key, out string value)
    {
        return attributes.TryGetValue(key, out value!);
    }


    public override void UseAttributeId()
    {
        if (attributes.TryGetValue("zhapi_id", out var zhapiId) && int.TryParse(zhapiId, out var index))
        {
            Index = index;
        }
        else if (attributes.TryGetValue("id", out var id) && int.TryParse(id, out index))
        {
            Index = index;
        }
        else
        {
            Index = -9527;
        }
    }

    /// <summary>
    /// <![CDATA[ <Title attr="xx" ]]> 不包含结尾
    /// </summary>
    /// <returns></returns>
    internal string GetNameXmlLine()
    {
        var items = Attributes.Select(x => $"{x.Key}=\"{x.Value.Trim()}\"").JoinString(" ");
        var end = items is { Length: > 0 } ? $" {items}" : "";
        return $"<{Name}{end}";

    }

    #region Code

    protected static IEnumerable<string> GetCodeLines(HtmlNode html)
    {
        var formatCode = HttpUtility.HtmlDecode(html.InnerText);
        return GetTabLines(formatCode);
    }

    protected static IEnumerable<string> GetTabLines(string html)
    {
        var codes = GetLines(html);
        var index = GetStartIndex(codes);
        return GetCodes(codes, index);
    }

    private static string[] GetLines(string code)
    {
        var array = Trim(code.GetLines());
        return Trim(array.Reverse()).Reverse().ToArray();
    }

    /// <summary>
    /// 1 <zhapi error="匹配失败" >错误</zhapi> 23
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    private static IEnumerable<string> Trim(IEnumerable<string> items)
    {
        var array = items.Index();
        var (index, v) = array
            .Where(x => x.Item.Trim().Length > 0)
            .FirstOrDefault();

        if (v is null) return items;
        return items.Skip(index);
    }

    private static int GetStartIndex(string[] codes)
    {
        foreach (var line in codes)
        {
            var txt = line.Trim();
            if (txt.Length is 0) continue;
            return line.IndexOf(txt);
        }
        return 0;
    }

    private static IEnumerable<string> GetCodes(string[] codes, int index)
    {
        foreach (var line in codes)
        {
            if (index > line.Length) yield return "\r\n";
            else yield return line[index..];
        }
    }
    #endregion

    #region Match

    /// <summary>
    /// 返回匹配数量(code 和 Comment匹配内容，ElementNode匹配特性attributes内容)
    /// </summary>
    private int GetMatchCount(AttributeNode node)
    {
        var matchItem = GetMatchItem();
        return node.GetMatchItem().Intersect(matchItem).Count();
    }

    private string[]? matchItem;
    /// <summary>
    /// 返回用于匹配的项
    /// </summary>
    private string[] GetMatchItem()
    {
        if (matchItem != null) return matchItem;
        var attrs = Attributes.Select(x => $"{x.Key}={x.Value}");

        const StringSplitOptions sso =
            StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries;

        var wds = CodeLines.JoinString().Split(' ', sso);
        return matchItem = wds.Concat(attrs).Distinct().ToArray();
    } 

    /// <summary>
    /// 返回配内容或者特性的项
    /// </summary>
    public NodeBase? GetMatchNode(IEnumerable<AttributeNode> nodes) =>
        nodes.Select(x => (x, GetMatchCount(x)))
        .OrderByDescending(x => x.Item2)
        .FirstOrDefault().x;

    #endregion
}