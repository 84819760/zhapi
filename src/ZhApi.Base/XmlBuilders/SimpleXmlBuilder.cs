
using Microsoft.Extensions.Logging;

namespace ZhApi.XmlBuilders;
/// <summary>
/// 
/// </summary>
/// <param name="name">
/// 有参数 <![CDATA[ <name id="1" /> ]]>
/// <br/>
/// 无参数  <![CDATA[ <see id="1" /> ]]>
/// </param>
public class SimpleXmlBuilder(string? name = null) : XmlBuilder
{
    private void DefaultHandler(AttributeNode node, XmlNode parent)
    {
        // -1024 = 导入时找不到原文中的节点
        if (node.Index != -1)
        {
            var xml = CreateElementAppend(name ?? node.Name, parent);
            xml.SetAttribute("id", node.Index.ToString());
        }
        else
        {
            try
            {
                CreateOriginal(node, parent);
            }
            catch (Exception ex)
            {
                FileLog.Default.LogError("""
                 node : {json}

                 Exception : {ex}
                 """, node.Serialize(true), ex.ToString());
                throw;
            }
        }
    }

    private void CreateOriginal(AttributeNode node, XmlNode parent)
    {
        var xml = doc.CreateElement(node.Name);
        parent.AppendChild(xml);

        foreach (var item in node.Attributes)
            xml.SetAttribute(item.Key, item.Value);
    }


    public override void CodeHandler(CodeNode node, XmlNode parent)
    {
        DefaultHandler(node, parent);
    }

    public override void ElementHandler(ElementNode node, XmlNode parent)
    {
        DefaultHandler(node, parent);
    }

    public override void CommentHandler(CommentNode node, XmlNode parent)
    {
        DefaultHandler(node, parent);
    }
}
