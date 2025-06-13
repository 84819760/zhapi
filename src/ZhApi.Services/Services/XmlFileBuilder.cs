namespace ZhApi.Services;
public class XmlFileBuilder(FileLog fileLog,
    IDbContextFactory<KvDbContext> dbFactory,
    CancellationTokenSource cts, FileData file)
{
    private readonly string tmpFilePath = $"{file.XmlFileInfo.ZhHans}.temp.xml";
    private readonly XmlFileInfo info = file.XmlFileInfo;
    private readonly CancellationToken token = cts.Token;
    private int memberCount;
    private int writeCount;
    private int warnCount;
    private int failCount;

    public async Task BuildAsync()
    {
        GC.Collect();
        if (info.AssemblyInfo.MemberCount is 0) return;
        var zhFile = info.ZhHans.FullName;
        var zh_dir = Path.GetDirectoryName(zhFile);
        try
        {
            if (zh_dir != null)
                Directory.CreateDirectory(zh_dir);

            await BuildXmlAsync();
            File.Move(tmpFilePath, zhFile, true);

            new DecreaseFile().SendMessage();

            fileLog.LogDebug("""
            写入文件
            数量  : {memberCount} ({writeCount}, warn :{warnCount}, fail:{failCount})
            源文  : '{file}'
            译文  : '{zhfile}'
            """, memberCount, writeCount, warnCount, failCount, file.XmlFileInfo.XmlFile.FullName, zhFile);
        }
        catch (UnauthorizedAccessException)
        {
            var msg = $"需要管理员权限运行，路径权限不足 ： {zh_dir}";
            new ExceptionMessage(msg).SendMessage();
            cts.TryCancel();
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            fileLog.LogError("""
            创建文件失败:
            path  '{path}'
            {ex}
            """, zhFile, ex.ToString());
            new ExceptionMessage($"{ex.Message} {zhFile}").SendMessage();
        }
    }

    private async Task BuildXmlAsync()
    {
        using var stream = new XmlFileStreamNode(tmpFilePath);
        await stream.WriteDocumentc(info, BuilderAsync);
    }

    private async Task BuilderAsync(XmlFileStreamNode stream)
    {
        var assemblyInfo = info.AssemblyInfo;
        var total = assemblyInfo.MemberCount;
        double progress = 0;

        foreach (var member in assemblyInfo.GetMemberNodes())
        {
            memberCount++;

            var p = memberCount.GetProgress(total);
            if (p != progress)
            {
                progress = p;
                new FileProgressMessage("写入文件", info, memberCount, total).SendMessage();
            }

            var originals = member.GetTextNodeParentDistinct();
            foreach (var original in originals)
            {
                if (token.IsCancellationRequested) return;
                await BuilderAsync(original);
            }
            await stream.WriteScope(() => stream.WriteNode(member));
        }

        new FileProgressMessage("写入文件", info, memberCount, total).SendMessage();
    }

    private async Task BuilderAsync(ElementNode original)
    {
        if (token.IsCancellationRequested) return;
        var errors = new List<string>();

        if (original.Xml.Key.IsContainChinese()) return;

        var replaceNodes = await CreateNode(original, errors)
             ?? CreateDefaultNode(original, errors);

        // 移除单词异常
        if (errors.Count is 1 && errors.Any(x => x.StartsWith("单词")))
            errors.Clear();

        if (errors.Count is 0)
        {
            original.ReplaceNodes = replaceNodes;
            writeCount += 1;
        }
        else if (errors.Contains("翻译失败"))
        {
            failCount += 1;
            var zhapi = new ZhApiWarnNode(original.Xml.OriginalXml, errors)
            {
                Name = "zhapi",
            };
            original.ReplaceNodes = [zhapi];
        }
        else
        {
            warnCount += 1;
            var zhapi = new ZhApiWarnNode(original.Xml.OriginalXml, errors)
            {
                Name = "zhapi",
                Nodes = replaceNodes,
            };
            original.ReplaceNodes = [zhapi];
        }
    }

    private async Task<List<NodeBase>?> CreateNode(ElementNode originalNode, List<string> errors)
    {
        var kvRow = await GetKvRowAsync(originalNode);
        if (kvRow == null)
            return TryCreateNode(originalNode);

        var newNode = UnitNode.CreateElementNode(kvRow.Translation)
            .Set(x => x.UseAttributeId());

        var res = SyncIndex(originalNode, newNode, errors).ToList();

        // 排除少数单词
        if (kvRow.Score > 1) errors.Add(kvRow.Tag);

        return res;
    }

    //部分情况下输入的节点不符合翻译条件
    private static List<NodeBase>? TryCreateNode(ElementNode originalNode)
    {
        var textNodes = originalNode.GetAllNodes().OfType<TextNode>();
        var wordHelper = WordHelper.Create(textNodes);
        if (wordHelper.Count > 0) return default;
        return originalNode.Nodes;
    }

    private static IEnumerable<NodeBase> SyncIndex(
        ElementNode originalNode, ElementNode newNode,
        List<string> errors)
    {
        var map = originalNode.GetIndexMap();
        foreach (var node in newNode.Nodes)
            yield return MatchNode(node, map, errors);
    }

    private static NodeBase MatchNode(NodeBase node,
        Dictionary<int, AttributeNode> map,
        List<string> errors)
    {
        if (node is not AttributeNode attribute)
            return node;

        var index = attribute.Index;

        if (map.TryGetValue(index, out var original))
            return original;

        attribute.Name = "zhapi";
        errors.Add(attribute["error"] = $"匹配索引失败{index}");
        return attribute;
    }

    private async Task<KvRow?> GetKvRowAsync(ElementNode originalNode)
    {
        var id = originalNode.Xml.Index;
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.KvRows.FirstOrDefaultAsync(x => x.Id == id && x.Score < 999);
    }

    private static List<NodeBase> CreateDefaultNode(ElementNode original, List<string> errors)
    {
        errors.Add("翻译失败");
        return original.Nodes;
    }
}
