#pragma warning disable
using Microsoft.Extensions.Logging;

namespace ZhApi.XmlBuilders;
public abstract class XmlBuilder
{
    protected readonly XmlDocument doc = new();

    /// <summary>
    /// 使用替换节点创建XML
    /// </summary>
    protected bool isReplaceNodes;

    /// <summary>
    /// 使用原始节点创建XML
    /// </summary>
    public virtual XmlElement Create(ElementNode node)
    {
        var root = doc.CreateElement(node.Name);
        var items = GetReplaceNodes(node);

        foreach (var item in node.Attributes)
            root.SetAttribute(item.Key, item.Value);

        CreateChild(items.ToArray(), root);

        return root;
    }

    /// <summary>
    /// 使用替换节点创建XML
    /// </summary>
    public virtual XmlElement CreateReplaceNodes(ElementNode node)
    {
        isReplaceNodes = true;
        return Create(node);
    }


    protected void CreateChild(NodeBase[] items, XmlNode parent)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (i > 0)
            {
                var txt = doc.CreateWhitespace(" ");
                parent.AppendChild(txt);
            }
            items[i].XmlBuild(this, parent);
        }
    }

    public virtual void TextHandler(TextNode node, XmlNode parent)
    {
        var item = doc.CreateTextNode(node.Text);
        parent.AppendChild(item);
    }

    public abstract void CodeHandler(CodeNode node, XmlNode parent);

    public abstract void CommentHandler(CommentNode node, XmlNode parent);   

    public abstract void ElementHandler(ElementNode node, XmlNode parent);

    protected XmlElement CreateElementAppend(string name, XmlNode parent)
    {
        var res = doc.CreateElement(name);
        parent.AppendChild(res);
        return res;
    }


    /// <summary>
    /// 优先返回替换的节点
    /// </summary>
    public IEnumerable<NodeBase> GetReplaceNodes(ElementNode node)
    {
        // 取消 生成XML部分修为自定义
        //if (isReplaceNodes && node.ReplaceNodes is { Length: > 0 })
        //    return node.ReplaceNodes;

        return node.Nodes;
    }
}