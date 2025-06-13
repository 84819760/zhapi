using System.Buffers;

namespace ZhApi.Services;

public record ImportXmlFileData(
    Action<int> SendTotal,
    Action<int> SendValue,
    KvRowService KvRowService,
    XmlFileInfo Info);

public class ImportXmlFile(
    SourceNameService sourceNameService,
    CancellationTokenSource cts,
    FileLog fileLog,
    ImportXmlFileData data)
{
    private const string modelName = "文件导入";
    private readonly static ModelInfo modelInfo = new(modelName, modelName);
    private readonly SearchValues<string> searchValues =
    SearchValues.Create(
    [
        "(错误:[",
        "无翻译数据，需要开启API"
    ], StringComparison.OrdinalIgnoreCase);


    private readonly Dictionary<string, SummaryNode> map = [];
    private readonly CancellationToken token = cts.Token;
    private int warningCount;

    public async Task RunAsync()
    {
        var info = data.Info;
        var aInfo = info.AssemblyInfo;
        var sourceId = await sourceNameService
            .GetSourceIdAsync(modelName);

        // 读取原始节点
        await Task.Run(() => HandlerAsync(aInfo, InitDictAsync));

        var zhFile = new XmlFileInfo(info.ZhHans.FullName).AssemblyInfo;
        await Task.Run(() => HandlerAsync(zhFile, x => ZhHansHandler(x, sourceId)));

        fileLog.LogInformation("""
        导入文件
        original : '{o}'
        zh-hans  : '{t}'
        error    : {count}
        """, info.XmlFile.FullName, info.ZhHans.FullName, warningCount);
    }

    private async Task InitDictAsync(SummaryNode original)
    {
        await Task.CompletedTask;
        var path = original.Info.MemberPath.Trim();
        map.TryAdd(path, original);
    }

    private async Task HandlerAsync(XmlAssemblyInfo info, Func<SummaryNode, Task> actino)
    {
        var index = 0;
        data.SendTotal(await info.GetMemberCount());
        foreach (var member in info.GetMemberNodes())
        {
            data.SendValue(index);
            foreach (var item in member.Nodes.OfType<SummaryNode>())
            {
                if (token.IsCancellationRequested) return;
                await actino(item);
            }
            index++;
        }
    }

    private async Task ZhHansHandler(SummaryNode zhNode, long sourceId)
    {
        var path = zhNode.Info.MemberPath.Trim();
        if (!map.TryGetValue(path, out var original)) return;
        var zh = ChildNodeHelper.GetMap(zhNode);
        foreach (var item in ChildNodeHelper.GetMap(original))
        {
            if (zh.TryGetValue(item.Key, out var zhItem))
                await ZhHansHandler(zhItem, item.Value, sourceId);
        }
    }

    private async Task ZhHansHandler(ElementNode zhNode, ElementNode original, long sourceId)
    {
        var originalNodes = GetAttributeNodes(original).OfType<NodeBase>().ToList();
        var zhNodes = GetAttributeNodes(zhNode);
        // 重设Index
        foreach (var zh in zhNodes)
        {
            if (GetMatchNode(zh, originalNodes) is NodeBase nb)
            {
                zh.Index = nb.Index;
            }
            else
            {
                /*
                <member name="M:System.Threading.CountdownEvent.Signal">
                原文
                <returns>true if the signal caused the count to reach zero and the event was set; otherwise, false.</returns>
                </member>

                zh-hans :
                <returns>如果信号导致计数变为零并且设置了事件，则为 <see langword="true" />；否则为 <see langword="false" />。</returns>
                 */
                zh.Index = -1;
                warningCount++;
            }
        }

        try
        {
            await SaveAsync(sourceId, original, zhNode);
        }
        catch (Exception ex)
        {
            fileLog.LogError("""
            original    : {original}
            translation : {translation}
            ex          : {ex}
            """, original.Xml.Key, zhNode.Xml.Key, ex.ToString());
        }
    }

    // 移除匹配项
    private static NodeBase? GetMatchNode(AttributeNode node, List<NodeBase> nodes)
    {
        var res = node.GetMatchNode(nodes.OfType<AttributeNode>());
        if (res != null) nodes.Remove(res);
        return res;
    }

    private static AttributeNode[] GetAttributeNodes(ElementNode element) =>
        [.. element.Nodes.OfType<AttributeNode>()];

    /* .net 官方文件中的翻译对不上？
    netstandard2.1\\netstandard.xml
    原文
    <member name="M:System.ArraySegment`1.System#Collections#Generic#ICollection{T}#AddFile(`0)">
        <summary>Adds an item to the array segment.</summary>
    </member>

    zh-hans :
    <member name="M:System.ArraySegment`1.System#Collections#Generic#ICollection{T}#AddFile(`0)">
        <summary>在所有情况下都会引发 <see cref="T:System.NotSupportedException" /> 异常。</summary>
    </member>
    */

    private async Task SaveAsync(long sourceId,
        ElementNode originalNode, ElementNode newNode)
    {
        var original = originalNode.Xml.Key;
        var translation = newNode.Xml.Key;

        if (original.Length is 0 || translation.Length is 0) return;
        // 无中文时退出
        if (!translation.IsContainChinese()) return;

        // 移除中文翻译节点
        if (original.IsContainChinese()) return;

        // 移除历史翻译中包含错误的节点
        if (translation.ContainsAny(searchValues)) return;

        // 原文中没有目标单词时退出(和翻译行为对齐)
        if (originalNode.WordKeys.Count is 0) return;

        // 无法获取评估数据时丢弃
        var sd = GetScoreData(originalNode, newNode);
        if (sd is null) return;

        await SaveAsync(sourceId, sd,
            originalNode.Info,
            originalNode.Xml.Index,
            original, translation);
    }

    private static ScoreData? GetScoreData(ElementNode originalNode, ElementNode newNode)
    {
        var pd = new RootData()
        {
            Index = originalNode.Xml.Index,
            OriginalXml = originalNode.Xml.Key,
            PathInfo = originalNode.Info
        };
        pd.CreateScore(modelInfo, newNode.Xml.Key);
        return ((IRootData)pd).GetScore();
    }

    private async Task SaveAsync(long sourceId, ScoreData score,
        PathInfo pathInfo, string id, string original, string translation)
    {
        var kv = new KvRow()
        {
            Translation = translation,
            Tag = score.ErrorSimple,
            SourceId = sourceId,
            Original = original,
            Score = score.Value,
            Id = id,
        };

        if (kv.Score > 0)
        {
            var type = score.IsFail ? "⛔" : "";
            Log(type, score, pathInfo, original, translation);
        }

        await data.KvRowService.SendAsync(kv);
    }


    private void Log(string type, ScoreData score,
        PathInfo pathInfo, string original, string translation)
    {
        var info = data.Info;
        var fullName = info.XmlFile.FullName;
        var zhHans = info.ZhHans.FullName;
        var memberPath = pathInfo.MemberPath;

        fileLog.LogWarning("""
        message     : {type}'{err}'
        score       : {score}
        file        : '{file}'
        zh-hans     : '{zh-hans}'
        member      : '{member}'
        original    : '{original}'
        translation : '{translation}'
        """,
        type,
        score.ErrorSimple,
        score.Value,
        fullName,
        zhHans,
        memberPath,
        original,
        translation);
    }
}

#if 历史版本
(错误:[重复标记{id}])";
(错误:[找不到ID

!本地数据库中无翻译数据，需要开启API！
#endif