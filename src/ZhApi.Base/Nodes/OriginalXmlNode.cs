using ZhApi.XmlBuilders;

namespace ZhApi.Nodes;

public class OriginalXmlNode : NodeBase
{
    public override void UseAttributeId() { }

    internal override void XmlBuild(XmlBuilder builder, XmlNode parant) { }

    public required XmlNode Original { get; init; }

    public override async Task Write(XmlFileStreamNode stream)
    {
        await stream.Write("\r\n");
        await stream.Write(Original.InnerXml);
    }
}