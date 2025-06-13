namespace ZhApi.Cores;

public class FileData
{  
    private int count;

    public FileData(XmlFileInfo xmlFileInfo, string id)
    {
        XmlFileInfo = xmlFileInfo;
        GcHelper<FileData>.Increment();
    }

    ~FileData() => GcHelper<FileData>.Decrement();
  
    public XmlFileInfo XmlFileInfo { get; }

    public int Count { get; set; }

    public string XmlFile => XmlFileInfo.XmlFile.FullName;

    public HashSet<FileData> ReadAheadFiles { get; } = [];

    public bool IsCompleted => ReadAheadFiles.All(x => x.count == x.Count);

    public void TrySetResult() => Interlocked.Increment(ref count);
}
