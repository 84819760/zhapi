using System.Text.RegularExpressions;
using ZhApi.Bases;

namespace ZhApi.Cores;
public partial class WordHelper : ObjectPoolBase<WordHelper>
{
    private const StringSplitOptions splitOptions =
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    private readonly HashSet<string> includes;
    private readonly HashSet<string> ignores;

    public WordHelper()
    {
        var icec = IgnoreCaseEqualityComparer.Instance;
        includes = new(icec);
        ignores = new(icec);
    }

    public IReadOnlySet<string> Includes => includes;

    public IReadOnlySet<string> Ignores => ignores;

    public int Count => includes.Count;

    public bool IsIgnore(string value) => ignores.Contains(value);

    public bool IsInclude(string value) => includes.Contains(value);


    [GeneratedRegex(@"[A-Z]")]
    private static partial Regex RegexAZ();

    //[GeneratedRegex(@"[\.]?[a-z_A-Z]+[a-z_A-Z]*")]
    //public static partial Regex RegexWord();

    [GeneratedRegex(@"[0-9]")]
    public static partial Regex RegexNumeral();

    // 英文字母开头
    [GeneratedRegex(@"^[a-z_A-Z]+[a-z_A-Z_0-9]*")]
    public static partial Regex RegexStart();

    // 移除末尾符号
    [GeneratedRegex(@"[^a-zA-Z]+$")]
    public static partial Regex RegexEnd();

    // 符号
    [GeneratedRegex(@"[\p{P}\p{S}]")]
    public static partial Regex RegexSymbol();


    // 检查大写字母只能在最前面
    private static bool CheckUpper(string text)
    {
        var last = RegexAZ().Matches(text).LastOrDefault();
        return last is null || last.Index is 0;
    }

    // 以英文开头
    private static bool CheckStart(string text) =>
        RegexStart().IsMatch(text);

    // 符号数量
    private static int GetSymbolCount(string text) =>
        RegexSymbol().Matches(text).Count;

    public static WordHelper Create(params IEnumerable<TextNode> nodes)
    {
        var res = CreateInstacne();
        res.Append(nodes.Select(x => x.Text));
        return res;
    }

    public WordHelper Append(IEnumerable<string> texts)
    {
        foreach (var text in texts) Append(text);
        return this;
    }

    public WordHelper Append(string text)
    {
        var items = text.Split(' ', splitOptions);
        foreach (var item in items)
        {
            // 移除结尾的符号
            var txt = RegexEnd().Replace(item, "").TrimStart('(');
            if (txt.Length < 2) continue;

            if (IgnoreWords.Contains(txt) ||
                // 英文开头
                !CheckStart(txt) ||
                // 大写检查
                !CheckUpper(txt) ||
                // 符号检查
                GetSymbolCount(txt) > 0 ||
                // 包含中文
                txt.IsContainChinese() ||
                // 包含数字
                RegexNumeral().IsMatch(txt))
            {
                ignores.Add(txt);
            }
            else
            {
                includes.Add(txt);
            }
        }
        return this;
    }

    /// <summary>
    /// 返回到池中
    /// </summary>
    public override void ReturnPool()
    {
        includes.Clear();
        ignores.Clear();
        Return(this);
    }
}