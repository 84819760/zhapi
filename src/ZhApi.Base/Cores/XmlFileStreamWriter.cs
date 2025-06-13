namespace ZhApi.Cores;
public class XmlFileStreamWriter : IDisposable
{
    private readonly StreamWriter stream;
    private readonly string tmpFilePath;

    public XmlFileStreamWriter(XmlFileInfo info)
    {
        tmpFilePath = $"{info.ZhHans}.temp.xml";
        //incompleteFilePath = MemberPath.ChangeExtension(info.ZhHans.FullName, ".zhapi_incomplete.json");
        stream = new(tmpFilePath) { AutoFlush = true };
        Writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Indent = true,
            Async = true,
            ConformanceLevel = ConformanceLevel.Document,
            OmitXmlDeclaration = false,
        });
        Init(info);
    }

    public List<object> Incompletes { get; } = [];

    public string TempPath => tmpFilePath;

    public XmlWriter Writer { get; }

    private void Init(XmlFileInfo info)
    {
        var assemblyName = info.AssemblyInfo.AssemblyName;
        Writer.WriteStartDocument();

        Writer.WriteStartElement("doc");
        Writer.WriteStartElement("assembly");

        Writer.WriteElementString("name", assemblyName);
        Writer.WriteEndElement();

        Writer.WriteStartElement("members");       
    }

    public void Dispose()
    {
        Writer.WriteEndElement();
        Writer.WriteEndElement();
        Writer.WriteEndDocument();

        Writer.Dispose();
        stream.Dispose();
        GC.SuppressFinalize(this);
    }
}