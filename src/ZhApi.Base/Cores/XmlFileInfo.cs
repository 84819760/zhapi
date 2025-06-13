using System.Diagnostics;

namespace ZhApi.Cores;

[DebuggerDisplay("{XmlFile.FullName}")]
public class XmlFileInfo
{
    private XmlAssemblyInfo? assemblyInfo;

    public XmlFileInfo(string path) : this(new FileInfo(path)) { }

    public XmlFileInfo(FileInfo xmlFile)
    {
        XmlFile = xmlFile;

        var dll = Path.ChangeExtension(xmlFile.FullName, "dll");
        Dll = new FileInfo(dll);

        var zhPath = Path.Combine(xmlFile.DirectoryName!, "zh-Hans", xmlFile.Name);
        ZhHans = new FileInfo(zhPath);

        var zhApiPath = Path.ChangeExtension(zhPath, "zhapi.log");
        ZhApiLog = new FileInfo(zhApiPath);
    }

    public FileInfo XmlFile { get; }

    public FileInfo ZhHans { get; }

    public FileInfo ZhApiLog { get; }

    public FileInfo Dll { get; }

    public XmlAssemblyInfo AssemblyInfo => assemblyInfo ??= new XmlAssemblyInfo(this);
}