using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace ZhApi.Cores;

public partial class RepairXml
{
    private static void Show(HtmlNode node, int tab)
    {
        foreach (var item in node.ChildNodes)
        {
            Console.Write(new string(' ', tab));
            if (item is HtmlTextNode tNode)
            {
                Console.WriteLine(tNode.Text);
            }
            else if (item is HtmlNode hn)
            {
                Console.WriteLine(hn.Name);
                Show(hn, tab + 1);
            }
        }
    }

    internal static void Test()
    {
        var xml =
        """
        </v id="5" />    
        """;
        var p = @"\<+\/+\s*v\s+id\s*\=\s*""";
        var sb = new StringBuilder(xml);
        var items = Regex.Matches(xml, p);
        foreach (Match item in items)
        {
            Console.WriteLine($"-{item.Value}-");
            var index = item.Value.IndexOf('/');
            sb.Remove(index, 1);
        }
        Console.WriteLine(sb);
    }

    private static string Repair(string value, Func<Regex> getRegex,
        Action<IEnumerable<Match>, StringBuilder> hanlder)
    {
        var items = getRegex().Matches(value);
        if (items.Count == 0) return value;
        return Extends.StringBuild(sb =>
        {
            sb.Append(value);
            hanlder(items.Reverse(), sb);
        });
    }

    #region 左引号 (v id = 123) -> (v id =" 123)
    [GeneratedRegex(@"v\s+id\s*\=\s*\d+")]
    private static partial Regex RegexQuotationMarkStart();
    private static string RepairQuotationMarkStart(string value) =>
    Repair(value, RegexQuotationMarkStart, (items, sb) =>
    {
        foreach (Match item in items)
        {
            var index = item.Value.IndexOf('=') + 1;
            sb.Insert(index + item.Index, '"');
        }
    });
    #endregion

    #region 右引号 (v id =" 123) -> (v id =" 123")

    // 右侧引号 (v id =" 123) -> (v id =" 123")
    [GeneratedRegex(@"v\s+id\s*\=\s*""+\s*\d+(?!\s*"")(?!\d)")]
    private static partial Regex RegexQuotationMarkEnd();
    private static string RepairQuotationMarkEnd(string value) =>
    Repair(value, RegexQuotationMarkEnd, (items, sb) =>
    {
        foreach (Match item in items)
            sb.Insert(item.Index + item.Value.Length, '"');
    });
    #endregion

    public static string RepairQuotationMark(string value)
    {
        var res = RepairQuotationMarkStart(value);
        return RepairQuotationMarkEnd(res);
    }

    #region 左括号 (v id = "123" />) -> (<v id = "123")
    [GeneratedRegex(@"(?<!\<\s*)v\s+id\s*\=\s*""+\s*\d+\s*""+\s*/*>")]
    private static partial Regex RegexBracesStart();
    private static string RepairRegexBracesStart(string value) =>
    Repair(value, RegexBracesStart, (items, sb) =>
    {
        foreach (Match item in items)
            sb.Insert(item.Index, '<');
    });

    #endregion

    #region 右括号 (<v id = "123") -> (<v id = "123" />)
    [GeneratedRegex(@"\<+\s*v\s+id\s*\=\s*""+\s*\d+\s*""+(?!\s*/*>)")]
    private static partial Regex RegexBracesEnd();
    public static string RepairRegexBracesEnd(string value) =>
    Repair(value, RegexBracesEnd, (items, sb) =>
    {
        foreach (Match item in items)
        {
            var index = item.Index + item.Value.Length;
            sb.Insert(index, "/>");
        }
    });
    #endregion

    #region 左括号 (< v id = "123) -> (<v id = "123)   
    [GeneratedRegex(@"<\s+v\s+id\s*\=\s*""+\s*\d+")]
    private static partial Regex RegexVid();
    public static string RepairRegexVid(string value) =>
    Repair(value, RegexVid, (items, sb) =>
    {
        foreach (Match item in items)
        {
            var count = item.Value.IndexOf('v') - 1;
            sb.Remove(item.Index + 1, count);
        }
    });
    #endregion

    #region 左括号 (v(id = "1")) -> (<v id = "123"/>)  
    [GeneratedRegex(@"v\s*[\(（]+\s*id\s*\=\s*""+\s*\d+\s*""+\s*[\)）]+")]
    private static partial Regex RegexVid2();
    public static string RepairRegexVid2(string value) =>
    Repair(value, RegexVid2, (items, sb) =>
    {
        foreach (Match item in items)
        {
            var v = item.Value;
            var l = v.IndexOfAny(['(', '（']);
            if (l is -1) return;
            var r = v.IndexOfAny([')', '）']);
            if (r is -1) return;

            sb[l] = ' ';
            sb[r] = ' ';
            sb.Insert(r, "/>");
            sb.Insert(item.Index, "<");
        }
    });
    #endregion

    #region (</v id="5") -> <v id="5"
    [GeneratedRegex(@"\<+\s*\/+\s*v\s+id\s*\=\s*""")]
    private static partial Regex Regex1();
    public static string RepairRegex1(string value) =>
    Repair(value, Regex1, (items, sb) =>
    {
        foreach (var item in items)
        {
            var index = item.Value.IndexOf('/');
            sb.Remove(index, 1);
        }
    });
    #endregion

    #region 不可见字符
    [GeneratedRegex(@"[\p{C}\p{Z}-[ \r\n]]")]
    private static partial Regex RegexWhiteSpace();

    public static string WhiteSpace(string value) =>
    Repair(value, RegexWhiteSpace, (items, sb) =>
    {
        foreach (Match item in items)
            sb[item.Index] = ' ';
    });

    #endregion

    #region v id="1”
    [GeneratedRegex(@"v\s+id\s*=\s*[“”‘’""]+\s*\d+\s*[“”‘’""]+")]
    private static partial Regex Regex2();

    [GeneratedRegex(@"[“”‘’]")]
    private static partial Regex Regex21();

    public static string RepairRegex2(string value) =>
    Repair(value, Regex2, (items, sb) =>
    {
        foreach (Match item in items)
        {
            var targets = Regex21().Matches(item.Value);
            foreach (var v in targets.Reverse())
                sb[v.Index + item.Index] = '"';
        }
    });
    #endregion

    #region IsHtmlEncode

    //[GeneratedRegex(@"\&lt\;.*?\&gt\;")]
    [GeneratedRegex(@"\&lt\;\s?v\s+id=.*?\&gt\;")]
    private static partial Regex RegexXmlEncode();
    private static string ReplaceVid(string value)
    {
        var rs = RegexXmlEncode().Matches(value).Reverse().ToArray();
        if (rs.Length is 0) return value;
        var res = Extends.StringBuild(sb =>
        {
            sb.Append(value);
            foreach (var item in rs)
            {
                var txt = item.Value;
                var newTxt = HttpUtility.HtmlDecode(txt);
                sb.Remove(item.Index, txt.Length);
                sb.Insert(item.Index, newTxt);
            }
        });
        return res;
    }

    #endregion

    [GeneratedRegex(@"(?<=[\u4E00-\u9FFF]+)\s+(?=[\u4E00-\u9FFF]+)")]
    private static partial Regex ZhSpaceRegex();

    public static string ZhSpace(string value) => Extends.StringBuild(sb =>
    {
        sb.Append(value);
        var items = ZhSpaceRegex().Matches(value).Reverse();
        foreach (var item in items)
            sb.Remove(item.Index, item.Length);
    });


    public static string Repair(string value)
    {
        var res = Replace(value);
        res = WhiteSpace(res);
        res = ReplaceVid(res);

        // v id="1”
        res = RepairRegex2(res);

        // 补充双引号
        res = RepairQuotationMark(res);

        // </v id ="
        res = RepairRegex1(res);

        // 补充 尖括号
        res = RepairRegexBracesStart(res);
        res = RepairRegexBracesEnd(res);

        // < v
        res = RepairRegexVid(res);

        // v(id = "1")
        res = RepairRegexVid2(res);

        res = ZhSpace(res);

        if (value != res)
        {
            var msg = $"""
                尝试修复:
                原文: 
                {value}
                
                返回: 
                {res}

                """;

            FileLog.Default.LogTrace(msg);

        }
        return res;
    }

    private static string Replace(string value) => value
    .Replace("<根>", "<root>")
    .Replace("</根>", "</root>")
    .Replace("＝", "=")
    .Replace("＜", "<")
    .Replace("＞", ">")

    .Replace("/ >", "/>")
    .Replace("／>", "/>")
    .Replace("\\>", "/>")
    .Replace(">\\", ">")

    .Replace("<_v id=", "<v id=")
    .Replace("<id=\"", "<v id=\"")
    .Replace("\\<v id=", "<v id=")
    .Replace("<\\v id=", "<v id=")

    .Replace("《v id=", "<v id=")
    .Replace("\" /》", "\" />")
    ;
}

/* 待补充
v:id="1"
</v id="5" />

```xml
<directory>A <v id="1" /> that represents the space between the edge of a <v id="3" /> and its content.</directory>
```
响应: 
```xml
<directory>一条<v id="1" />，表示边缘的 v<v id="3" /> 及其内容之间的空间。</directory>
```
 */

/* 无解
```xml
<directory>
A <v id="1" /> that represents the style of the border of the upper-left cell in the <v id="3" /> .
</directory>
```
响应: 
```xml
<directory>一
个具有id为“1”的标签(<v id="1" />)，它代表了上左单元格边框的风格（在<v id="3" />中）。
</directory>


```xml
<directory>A <v id="1" /> that represents the default property for the current object, or <v id="3" /> if the object does not have properties.</directory>
```
响应: 
```xml
<directory>一个具有id为“1”的标签，代表了当前对象的默认属性，如果对象没有属性，则使用具有id为“3”的标签。</directory>
```
```
 */