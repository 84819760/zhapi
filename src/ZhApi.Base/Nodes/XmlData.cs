using ZhApi.XmlBuilders;

namespace ZhApi.Nodes;
public class XmlData(ElementNode node)
{
    private XmlNode? originalXml;
    private string? xmlKey;
    private string? index;

    /// <summary>
    /// 用于查询key <![CDATA[ <v id="1" /> ]]>
    /// </summary>
    public string Key =>
        xmlKey ??= new SimpleXmlBuilder("v").Create(node).InnerXml.Trim();


    public string Request => Key;

    /// <summary>
    /// 生成原始XML，用于生成文件
    /// </summary>
    public virtual XmlNode OriginalXml =>
        originalXml ??= new OriginalXmlBuilder().CreateReplaceNodes(node);

    public string Index => index ??= Key.GetSha256Base64();

}