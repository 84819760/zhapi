using HtmlAgilityPack;
using System.Text.Json.Serialization;
using ZhApi.XmlBuilders;

namespace ZhApi.Nodes;

public abstract class NodeBase
{
    public NodeBase? Parent { get; set; } = null!;

    public PathInfo Info { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Index { get; set; } = -1;

    public virtual IEnumerable<NodeBase> GetAllNodes() => [this];

    public NodeBase Set(HtmlNode html, PathInfo info, NodeBase? parent)
    {
        Name = html.Name.Trim();
        Info = info;
        Parent = parent!;
        return this;
    }

    internal abstract void XmlBuild(XmlBuilder builder, XmlNode parant);

    /// <summary>
    /// 从特性中获取ID替换当前节点的Index
    /// </summary>
    public abstract void UseAttributeId();

    public virtual void SetAttribute(string name, string value) { }

    public abstract Task Write(XmlFileStreamNode stream);
   
}