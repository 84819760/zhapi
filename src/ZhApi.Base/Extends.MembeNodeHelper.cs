using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace ZhApi;
public partial class MembeNodeHelper
{
    private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
    private const string endMember = "</members>";
    private const string member = "member";

    [GeneratedRegex(@"<\s*\/\s*(?i)member\s*>")]
    private static partial Regex endRegex();

    public static IEnumerable<HtmlNode> GetMembers(string file)
    {
        var sb = new StringBuilder();
        foreach (var line in File.ReadLines(file))
            foreach (var xml in GetMembers(sb, line))
                foreach (var item in GetHtmlNodes(xml))
                    yield return item;
    }

    static IEnumerable<string> GetMembers(StringBuilder sb, string line)
    {
        if (line.Contains(endMember, IgnoreCase))
        {
            sb.AppendLine(line);
            return [sb.ToString()];
        }

        if (!line.Contains(member, IgnoreCase)) return Get();

        // 匹配结束
        var end = endRegex().Matches(line).LastOrDefault();
        if (end is null) return Get();
        return GetLines(end, sb, line);

        IEnumerable<string> Get()
        {
            sb.AppendLine(line);
            return [];
        }
    }

    static string[] GetLines(Match item, StringBuilder sb, string line)
    {
        var xml = Capture(item, line, out var index);
        sb.AppendLine(xml);
        var res = sb.ToString();
        sb.Clear();

        var next = line[index..];
        sb.AppendLine(next);
        return [res];
    }

    static string Capture(Match item, string line, out int index)
    {
        index = item.Index + item.Length;
        return line[..index];
    }

    static IEnumerable<HtmlNode> GetHtmlNodes(string xml)
    {
        try
        {
            var doc = Extends.ToHtmlDocument(xml);
            return doc.DocumentNode.SelectNodes("//member")?.Cast<HtmlNode>() ?? [];
        }
        catch (Exception)
        {
            return [];
        }    
    }
}