using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ZhApi.Cores;

[DebuggerDisplay("{AssemblyName} : {MemberCount}")]
public class XmlAssemblyInfo
{
    private string? assemblyName;
    private readonly Lazy<Task<int>> memberCountLazy;

    public XmlAssemblyInfo(XmlFileInfo xmlFileInfo)
    {
        XmlFileInfo = xmlFileInfo;
        memberCountLazy = new(CreateMemberCount);
    }

    /// <summary>
    /// 成员总数
    /// </summary>
    public int MemberCount => memberCountLazy.Value.Result;   

    private Task<int> CreateMemberCount() =>
        Task.Run(() => MemberCountHelper.GetMemberCountAsync(FullName));

    public Task<int> GetMemberCount() => memberCountLazy.Value;

    public string AssemblyName => assemblyName ??= GetAssembly();

    public XmlFileInfo XmlFileInfo { get; }

    public string FullName => XmlFileInfo.XmlFile.FullName;

    private IEnumerable<HtmlNode> GetHtmlNodes() =>
           MembeNodeHelper.GetMembers(FullName);

    private string GetAssembly()
    {
        const string defalutValue = "未知";
        try
        {
            return GetHtmlNodes()?.FirstOrDefault()?
                .SelectSingleNode("//assembly/name")?
                .InnerText?.Trim() ?? defalutValue;
        }
        catch (Exception)
        {
            return defalutValue;
        }
    }

    public IEnumerable<MemberNode> GetMemberNodes()
    {
        var id = new IdProvider();
        foreach (var html in GetHtmlNodes())
        {
            if (CreateNode(html, id.GetId(), out var res))
                yield return res;
        }
    }

    private bool CreateNode(HtmlNode html, int id, out MemberNode node)
    {
        try
        {
            node = MemberNode.Create(this, html, id);
            // 删除奇怪的节点
            RemoveTextNode(node);
        }
        catch (Exception ex)
        {
            node = null!;
            FileLog.Default.LogError("""
                读取文件时发生错误: {ex}
                file :{path}
                xml:{xml}
                """, ex.ToString(), FullName, html.OuterHtml);
        }
        return node != null;
    }

    private static void RemoveTextNode(MemberNode memberNode)
    {
        var nodes = memberNode.Nodes.OfType<TextNode>().ToArray();
        foreach (var x in nodes)
            memberNode.Nodes.Remove(x);
    }
}