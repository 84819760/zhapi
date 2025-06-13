namespace ZhApi.XmlBuilders;
/// <summary>
/// 输出完整的XML用于生成文件
/// </summary>
public class OriginalXmlBuilder : XmlBuilder
{
    public override void CodeHandler(CodeNode node, XmlNode parent) =>
        Handler(node, parent);

    public override void ElementHandler(ElementNode node, XmlNode parent)
    {
        var res = Handler(node, parent);
        var items = GetReplaceNodes(node).ToArray();
        CreateChild(items, res);
    }

    private XmlElement Handler(AttributeNode node, XmlNode parent)
    {
        if (node.IsCodeNode && !node.IsSimple)
            parent.AppendChild(doc.CreateWhitespace("\r\n"));

        var res = CreateElementAppend(node.Name, parent);

        // 设置特性
        foreach (var item in node.Attributes)
            res.SetAttribute(item.Key, item.Value);

        // 代码节点
        if (node.IsCodeNode)
            res.InnerText = node.CodeLines.JoinString("\r\n");

        return res;
    }

    public override void CommentHandler(CommentNode node, XmlNode parent)
    {
        var txt = node.CodeLines.JoinString("\r\n");
        var cData = doc.CreateCDataSection(txt);      
        parent.AppendChild(cData);      
    }
}