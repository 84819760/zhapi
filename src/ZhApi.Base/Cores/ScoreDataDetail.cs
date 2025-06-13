namespace ZhApi.Cores;
public class ScoreDataDetail
{
    public required ResponseData ResponseData { get; init; }

    #region prop
    /// <summary>
    /// 重复节点
    /// </summary>
    public int[] RepeatNode { get; internal set; } = [];

    /// <summary>
    /// 多余节点
    /// </summary>
    public int[] RedundantNode { get; internal set; } = [];

    /// <summary>
    /// 丢失节点
    /// </summary>
    public int[] LossNode { get; internal set; } = [];

    /// <summary>
    /// 未翻译单词
    /// </summary>
    public string[] Words { get; internal set; } = [];

    /// <summary>
    /// 关键错误
    /// </summary>
    public string? Error { get; internal set; }

    #endregion

    private string GetError(
       Func<string, int[], string> intArrayHander,
       Func<string, string[], string> strArrayHander,
       Func<string, string> errHander, string comma = ", ",
       string word = "单词", string loss = "丢失",
       string redundant = "多余", string repetition = "重复") =>
       Extends.StringBuild(sb =>
   {
       // 关键错误
       if (Error is { Length: > 0 })
           sb.Append(errHander(Error));

       // 单词
       Append(Words, word, strArrayHander);

       // 节点丢失
       Append(LossNode, loss, intArrayHander);

       // 多余
       Append(RedundantNode, redundant, intArrayHander);

       // 重复
       Append(RepeatNode, repetition, intArrayHander);

       void Append<T>(T[]? items, string name, Func<string, T[], string> hander)
       {
           if (items is not { Length: > 0 }) return;
           var v = hander(name, items);
           AppendComma(sb, comma).Append(v);
       }
   });

    public string GetErrorSimple() => GetError(
        (title, array) => $"{title}:{array!.Length}",
        (title, array) => $"{title}:{array!.Length}",
        err => $"{err}");

    private static StringBuilder AppendComma(StringBuilder sb, string comma)
    {
        if (sb.Length > 0) sb.Append(comma);
        return sb;
    }

    /// <summary>
    /// 返回重试的Message
    /// </summary>
    public string GetRetryMessage()
    {
        var v = GetError(GetInts, GetWords, GetErrorContent,
            comma: "",
            word: "多个单词未被翻译",
            loss: "xml节点id丢失",
            redundant: "多余的xml节点id",
            repetition: "xml节点id重复出现");

        static string GetInts(string title, int[] indexs) =>
            $"{title}：{indexs.Select(GetXml).JoinString("，")}。";

        static string GetWords(string title, string[] indexs) =>
            $"{title}：[{indexs.Select(x => $"'{x}'").JoinString()}]。";

        static string GetErrorContent(string value)
        {
            if (value.Contains("未包含中文"))
                return $"返回内容未包含中文。";

            return $"返回的XML格式不正确。";
        }

        static string GetXml(int id) => $"<v id=\"{id}\" />";

        if (v.Length is 0) return "";
        return $"{v}请重新翻译。";
    }

    /// <summary>
    /// 返回错误，列表方式
    /// </summary>
    public string GetErrorList() => errorList ??= GetError(
        (title, array) => $"{title}:[{array.JoinString()}]",
        (title, array) => $"{title}:[{array.JoinString()}]",
        err => $"{err}");

    private string? errorList;


    private KeyData CreateKeyData(string index, double value)
    {
        var isNodeLoss = LossNode is { Length: > 0 };
        var isWordLoss = Words is { Length: > 0 };
        var isFail = Error != null ||
            RepeatNode is { Length: > 0 } ||
            RedundantNode is { Length: > 0 };

        var isPerfect = !isFail && !isWordLoss && !isNodeLoss;

        return new KeyData()
        {
            IsNodeLoss = isNodeLoss,
            IsWordLoss = isWordLoss,
            IsPerfect = isPerfect,
            IsFail = isFail,
            Index = index,
            Value = value,
        };
    }

    internal ScoreData CreateScoreData(string xml, string index)
    {
        if (ResponseData.IsTimeout)
            Error = $"请求超时！";

        var isFail = Error != null ||
            ResponseData.Exception != null ||
            RepeatNode is { Length: > 0 } ||
            RedundantNode is { Length: > 0 };

        double value = (isFail ? 1000 : 0) +
            (RedundantNode?.Length ?? 0) * 100 +
            (RepeatNode?.Length ?? 0) * 100 +
            (LossNode?.Length ?? 0) * 10 +
            (Words?.Length ?? 0);

        var key = CreateKeyData(index, value);

        return new ScoreData()
        {
            ErrorSimple = GetErrorSimple(),
            Value = value,
            Detail = this,
            Xml = xml,
            Key = key,
        };
    }
}