using HtmlAgilityPack;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using ZhApi.XmlBuilders;

namespace ZhApi.Nodes;
[DebuggerDisplay("ElementNode : name={Name},index={Index},count={Nodes.Count}")]
public class ElementNode : AttributeNode
{
    private XmlData? xmlData;

    public List<NodeBase> Nodes { get; set; } = [];

    /// <summary>
    /// 用于替换的节点
    /// </summary>
    public List<NodeBase>? ReplaceNodes { get; set; }

    public override IEnumerable<NodeBase> GetAllNodes() =>
        Nodes.SelectMany(x => x.GetAllNodes()).Prepend(this);

    public IEnumerable<ElementNode> GetTextNodeParentDistinct() =>
        GetAllNodes().OfType<TextNode>().Select(x => x.Parent)
        .Where(x => x is not MemberNode)
        .Distinct().OfType<ElementNode>();

    public IEnumerable<UnitNode> GetSnippetUnitNodes() =>
        GetTextNodeParentDistinct()        
        .Select(x => new UnitNode(x));

    [JsonIgnore]
    public XmlData Xml => xmlData ??= new XmlData(this);

    /// <summary>
    /// 创建时发生的异常(用于判断节点有效)
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Index 对应的节点
    /// </summary>
    public Dictionary<int, AttributeNode> GetIndexMap() =>
        Nodes.OfType<AttributeNode>().ToDictionary(x => x.Index);

    #region WordHelper

    private WordHelper? wordKeys;

    /// <summary>
    /// 单词集合(小写集合)
    /// </summary>
    public WordHelper WordKeys => wordKeys ??= CreateWordHelper();

    private WordHelper CreateWordHelper() =>
        WordHelper.Create(GetAllNodes().OfType<TextNode>());

    #endregion

    #region override
    internal override void XmlBuild(XmlBuilder builder, XmlNode parant) =>
       builder.ElementHandler(this, parant);

    public override void UseAttributeId()
    {
        base.UseAttributeId();

        foreach (var item in Nodes)
            item.UseAttributeId();
    }
    #endregion

    #region CreateElement

    public void AddChild(NodeBase node)
    {
        var index = Nodes.Count;
        node.Index = index;
        Nodes.Add(node);
    }

    public static ElementNode CreateElement(HtmlNode html, PathInfo info, NodeBase parent)
    {
        // var res = NodePool<ElementNode>.Get(html, info, parent);
        var res = new ElementNode();
        res.Set(html, info, parent);
        res.SetAttribute(html.Attributes);

        foreach (var item in html.ChildNodes)
            CreateChildNodes(item, info, res, CreateElement);

        return res;
    }

    public static void CreateChildNodes(HtmlNode html, PathInfo info, ElementNode parent,
        Func<HtmlNode, PathInfo, ElementNode, ElementNode> createElement)
    {
        if (html.Name is "zhapi") return;

        if (html is HtmlCommentNode comm)
        {
            Add(CommentNode.Create(comm, info, parent));
        }
        else if (html is HtmlTextNode tNode)
        {
            if (tNode.Text.Trim().Length > 0)
                Add(TextNode.Create(tNode, info, parent));
        }
        else if (html.Name.ToLower().Trim() is "code" or "c")
        {
            Add(CodeNode.Create(html, info, parent));
        }
        else
        {
            Add(createElement(html, info, parent));
        }

        void Add(NodeBase node) => parent.AddChild(node);
    }

    #endregion

    #region XmlFile

    public override async Task Write(XmlFileStreamNode stream)
    {
        var start = GetNameXmlLine();
        var nodes = ReplaceNodes ?? [.. Nodes];
        await WriteStart(stream, start, nodes);

        foreach (var item in nodes)
            await WriteChildBefore(stream, item);

        await WriteEnd(stream, nodes);
    }

    private bool IsMiddle(NodeBase item) =>
        IsMiddle(ReplaceNodes, item) || IsMiddle(Nodes, item);

    private static bool IsMiddle(IEnumerable<NodeBase>? nodes, NodeBase item)
    {
        var ns = nodes ?? [];
        return item != ns.FirstOrDefault();
    }

    protected virtual async Task WriteChildBefore(XmlFileStreamNode stream, NodeBase item)
    {
        if (IsMiddle(item))
            await MiddleHandler(stream, item);
        await stream.WriteNode(item);
    }

    protected virtual Task MiddleHandler(XmlFileStreamNode stream, NodeBase item)
    {
        return stream.Write(" ");
    }

    protected virtual Task WriteStart(XmlFileStreamNode stream, string start, IReadOnlyList<NodeBase> nodes)
    {
        var end = nodes.Count is 0 ? $"/>" : ">";
        return stream.Write($"{start}{end}");
    }

    protected virtual Task WriteEnd(XmlFileStreamNode stream,
        IReadOnlyList<NodeBase> nodes)
    {
        if (nodes.Count is 0) return Task.CompletedTask;
        return stream.Write($"</{Name}>");
    }

    #endregion
}