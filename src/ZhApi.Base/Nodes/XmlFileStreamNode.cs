using System.CodeDom.Compiler;

namespace ZhApi.Nodes;
public class XmlFileStreamNode : IDisposable
{
    private readonly static Dictionary<int, string> tabs = [];
    private readonly IndentedTextWriter writer;
    private readonly StreamWriter stream;
    private readonly FileStream fs;

    public XmlFileStreamNode(string filePath)
    {
        fs = new(filePath, FileMode.OpenOrCreate);
        stream = new StreamWriter(fs);
        writer = new(stream);
    }

    public int Tab { get; set; } = -1;

    private string GetTab()
    {
        if (!tabs.TryGetValue(Tab, out var res))
            tabs[Tab] = res = new string(' ', Tab * 2);
        return res;
    }

    public void Dispose()
    {
        stream.Dispose();
        fs.Dispose();
        GC.SuppressFinalize(this);
    }

    public Task WriteDocumentc(XmlFileInfo info, Func<XmlFileStreamNode, Task> send)
    {
        return WriteDocumentc(info.AssemblyInfo, send);
    }

    private async Task WriteDocumentc(XmlAssemblyInfo assemblyInfo,
        Func<XmlFileStreamNode, Task> send)
    {
        await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        await WriteScope(async () =>
        {
            await writer.WriteLineAsync("<doc>");
            await WriteScope(async () =>
            {
                var name = $"<name>{assemblyInfo.AssemblyName}</name>";
                await WriteLine("<assembly>");
                await WriteScope(() => WriteLine(name));
                await WriteLine("</assembly>");
            });

            await WriteScope(async () =>
            {
                await WriteLine("<members>");
                await send(this);
                await WriteLine("</members>");
            });
            await WriteLine("</doc>");
        });
    }

    public async Task WriteTab(string value = "")
    {
        await writer.WriteAsync(GetTab());
        await writer.WriteAsync(value);
    }

    public async Task WriteLine(string value = "", bool writeTab = true)
    {
        if (writeTab) await WriteTab();
        await writer.WriteLineAsync(value);
    }

    public Task Write(string value = "") => writer.WriteAsync(value);

    public async Task WriteScope(Func<Task> action)
    {
        Tab++;
        await action();
        Tab--;
    }

    /// <summary>
    /// 不在Node中调用
    /// </summary>
    public Task WriteNode(NodeBase node) => node.Write(this);
}