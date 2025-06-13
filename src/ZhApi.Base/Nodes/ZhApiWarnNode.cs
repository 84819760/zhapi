namespace ZhApi.Nodes;

/// <summary>
/// 用于显示不完整翻译
/// </summary>
public class ZhApiWarnNode : ElementNode
{
    private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
    private readonly XmlNode original;

    public ZhApiWarnNode(XmlNode original, List<string> errors)
    {
        this.original = original;
        foreach (var e in errors.Index())
            this[$"e{e.Index}"] = e.Item;
    }

    protected async Task WriteStart(XmlFileStreamNode stream)
    {
        await stream.WriteLine();
        await stream.WriteTab();
        await stream.Write($"<{Name}");

        foreach (var x in Attributes)
            await stream.Write($" {x.Key}=\"{x.Value}\"");

        await stream.Write($">");
        await stream.WriteLine(writeTab: false);
    }

    public override async Task Write(XmlFileStreamNode stream)
    {       
        if (Nodes is { Count:>0})
            Nodes.Add(CreateBr());

        Nodes.AddRange
        ([
            new TextNode() { Text = "原文:" },
            new OriginalXmlNode() { Original = original },
        ]);

        await WriteStart(stream);

        // 写入翻译节点
        foreach (var item in Nodes)
        {
            await item.Write(stream);

            if (item.Name?.Equals("br", IgnoreCase) ?? false)
                await stream.WriteLine(writeTab: false);
        }

        // 写入原始节点

        await WriteEnd(stream);
    }

    protected async Task WriteEnd(XmlFileStreamNode stream)
    {
        await stream.WriteLine(writeTab: false);
        await stream.WriteLine($"</{Name}>");
        await stream.WriteLine(writeTab: false);
    }

    public static ElementNode CreateBr() => new() { Name = "br" };
}

