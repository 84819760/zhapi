using HtmlAgilityPack;
using System.Diagnostics;

namespace ZhApi.Nodes;
[DebuggerDisplay("MemberNode : name={Name},index={Index},count={Nodes.Count}")]
public class MemberNode : ElementNode
{
    public XmlAssemblyInfo AssemblyInfo { get; set; } = null!;

    public static MemberNode Create(XmlAssemblyInfo assemblyInfo, string xml, int index)
    {
        var htmlNode = xml.ToHtmlNode().SelectSingleNode("//member")
            ?? throw new Exception($"找不到member ：{xml}");

        return Create(assemblyInfo, htmlNode, index);
    }

    public static MemberNode Create(XmlAssemblyInfo assemblyInfo, HtmlNode htmlNode, int index)
    {
        var memberPath = htmlNode.GetAttributeValue("name", "");

        if (memberPath.Length is 0)
            memberPath = $"无名节点_{index}";

        var filePath = assemblyInfo.XmlFileInfo.XmlFile.FullName;
        var info = new PathInfo(filePath, memberPath);

        var res = new MemberNode
        {
            Name = htmlNode.Name,
            Index = index,
            Info = info
        };

        res.SetAttribute(htmlNode.Attributes);
        res.AssemblyInfo = assemblyInfo!;

        foreach (var item in htmlNode.ChildNodes)
            CreateChildNodes(item, info, res, SummaryNode.CreateSummary);

        return res;
    }

    protected override async Task WriteStart(XmlFileStreamNode stream,
        string start, IReadOnlyList<NodeBase> nodes)
    {
        await stream.WriteTab();
        await base.WriteStart(stream, start, nodes);
    }

    protected override async Task WriteEnd(XmlFileStreamNode stream,
        IReadOnlyList<NodeBase> nodes)
    {
        await stream.WriteLine(writeTab: false);
        await stream.WriteTab();
        await base.WriteEnd(stream, nodes);
        await stream.WriteLine(writeTab: false);
        await stream.WriteLine(writeTab: false);
    }

    protected override Task WriteChildBefore(XmlFileStreamNode stream, NodeBase item)
    {
        return stream.WriteScope(async () =>
        {
            await stream.WriteLine();
            await stream.WriteTab();
            await base.WriteChildBefore(stream, item);
        });
    }

    protected override Task MiddleHandler(XmlFileStreamNode stream, NodeBase item)
    {
        return Task.CompletedTask;
    }
}