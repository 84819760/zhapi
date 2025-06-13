using Microsoft.Extensions.Logging;

namespace ZhApi.Nodes;
public class UnitNode(ElementNode originalNode)
{
    /// <summary>
    /// 原始节点
    /// </summary>
    public ElementNode Node { get; } = originalNode;

    /// <summary>
    /// 用于查询key <![CDATA[ <see id="1" /> ]]>
    /// </summary>
    public string Key => Node.Xml.Key;

    public string Request => Node.Xml.Request;

    public WordHelper WordKeys => Node.WordKeys;

    public string Sha256Code => Node.Xml.Index;


    #region SnippetNode  

    /// <summary>
    /// 参考当前节点创建片段节点
    /// </summary>
    public ElementNode CreateSnippetNode(string xml, string root = "root")
    {
        var indexMap = Node.GetIndexMap();
        var refNode = CreateElementNode(xml, Node.Info, root);

        var res = new ElementNode
        {
            Name = Node.Name
        };

        foreach (var item in refNode.Nodes)
        {
            if (item is TextNode)
            {
                res.AddChild(item);
            }
            // 使用原始节点替换
            else if (indexMap.TryGetValue(item.Index, out var node))
            {
                res.AddChild(node);
            }
        }
        return res;
    }

    public static UnitNode CreateUnitNode(string xml, PathInfo info)
    {
        var res = CreateElementNode(xml, info);
        return new(res);
    }


    /// <summary>
    /// 无参考创建建片段节点
    /// </summary>
    public static ElementNode CreateElementNode(string xml,
        PathInfo? info = null, string root = "root")
    {
        info ??= PathInfo.Default;

        var res = new ElementNode
        {
            Name = root,
            Info = info,
        };

        var rootXml = xml.Contains($"<{root}>") ? xml : $"<{root}>{xml}</{root}>";

        try
        {
            var html = rootXml.Replace("<v id=\"", "<v zhapi_id=\"")
                .ToHtmlNode().SelectSingleNode($"//{root}");

            if (html is null) return res;

            foreach (var item in html.ChildNodes)
                ElementNode.CreateChildNodes(item, info, res, ElementNode.CreateElement);

            res.UseAttributeId();

            _ = res.Xml.OriginalXml;
        }
        catch (Exception ex)
        {
            res.Exception = ex;
            FileLog.Default.LogError("""
            转换XML失败
            filePath:   '{filePath}'
            memberPath: '{memberPath}'

            xml: 
            {xml}

            exception: 
            {ex}
            """, info.FilePath, info.MemberPath, rootXml, ex.ToString());
            res.Nodes.Clear();
        }
        return res;
    }

    #endregion

}