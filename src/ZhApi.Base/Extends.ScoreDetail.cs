using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using ZhApi.Configs;
using ZhApi.Interfaces;

namespace ZhApi;
public static class ScoreDataDetailHelper
{
    public static void CreateScore(this IRootData data, ModelInfo modelInfo, string translation)
    {
        var rd = new ResponseData()
        {
            Response = translation.GetXmlMarkdown(),
            ModelInfo = modelInfo,
            RequestId = 0,
            Messages = [],
        };
        rd.CreateScore(data);
    }


    public static void CreateScore(this ResponseData rd, IRootData data)
    {
        var scores = GetScoreDatas(rd, data).DistinctBy(x => x.Detail.GetErrorList());
        data.Items.AddRange(scores);
    }

    private static IEnumerable<ScoreData> GetScoreDatas(ResponseData rd, IRootData data)
    {
        var originalNode = UnitNode.CreateElementNode(data.OriginalXml, data.PathInfo);
        var items = GetHtmlNodes(data, rd).ToArray();
        foreach (var item in items)
        {
            var newNode = UnitNode.CreateElementNode(item.OuterHtml, data.PathInfo);
            var detail = new ScoreDataDetail() { ResponseData = rd };
            _ = new Helper(originalNode, newNode, detail);

            yield return detail.CreateScoreData(newNode.Xml.Key, data.Index);
        }
    }

    private static IEnumerable<HtmlNode> GetHtmlNodes(IRootData data, ResponseData rd)
    {
        try
        {
            return rd.Response.ToHtmlNode().SelectNodes("//root")?.Cast<HtmlNode>() ?? [];
        }
        catch (Exception ex)
        {
            FileLog.Default.LogError("""
            转换XML时发生错误！
            服务: {info}
            请求: {req}
            响应: {res}
            异常: {ex}
            """, rd.ModelInfo.Info, data.OriginalXml, rd.Response, ex.Message);
            return [];
        }
    }

}

file class Helper
{
    private readonly ElementNode originalNode;
    private readonly ScoreDataDetail detail;
    private readonly ElementNode newNode;

    private readonly int[] originalIndexs;
    private readonly int[] newIndexs;

    public Helper(ElementNode originalNode,
        ElementNode newNode, ScoreDataDetail detail)
    {
        originalIndexs = GetIndexs(originalNode);
        newIndexs = GetIndexs(newNode);

        this.originalNode = originalNode;
        this.newNode = newNode;
        this.detail = detail;

        Test();
    }

    private static int[] GetIndexs(ElementNode node) =>
        node.Nodes.OfType<AttributeNode>().Select(x => x.Index).ToArray();

    private void Test()
    {
        if (TestLength())
        {
            detail.Error = "疑似Thinking";
        }
        else if (newNode.Nodes.Count is 0)
        {
            detail.Error = "XML格式不正确";
        }
        else
        {
            // 测试单词
            TestWords();
            // 丢失节点
            TestLossNode();
            // 多余节点
            TestRedundant();
            //重复节点
            TestRepeat();
        }
    }

    /// <summary>
    /// 测试字符串长度
    /// </summary>
    private bool TestLength()
    {
        var o = GetText(originalNode).Length;
        var n = GetText(newNode).Length;
        return (n / o) > 4;
    }

    private static string GetText(ElementNode node) =>
        node.GetAllNodes().OfType<TextNode>().Select(x => x.Text).JoinString();

    private bool ContainChinese => newNode
      .GetAllNodes().OfType<TextNode>().Any(x => x.IsChinese);

    private void TestWords()
    {
        var original = originalNode.WordKeys;
        var newKeys = newNode.WordKeys;
        var words = newKeys.Includes
            .Where(x => !original.IsIgnore(x))
            .ToHashSet(IgnoreCaseEqualityComparer.Instance);

        detail.Words = [.. words];

        if (ContainChinese || words.Count < 10) return;
        detail.Error = "未包含中文";
    }

    /// <summary>
    /// 节点丢失
    /// </summary>
    private void TestLossNode()
    {
        var res = originalIndexs.Except(newIndexs).ToHashSet();
        detail.LossNode = [.. res];
    }

    /// <summary>
    ///  多余节点
    /// </summary>
    private void TestRedundant()
    {
        var res = newIndexs.Except(originalIndexs).ToHashSet();
        detail.RedundantNode = [.. res];
    }

    /// <summary>
    /// 重复节点
    /// </summary>
    private void TestRepeat()
    {
        detail.RepeatNode = newIndexs.GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
    }
}