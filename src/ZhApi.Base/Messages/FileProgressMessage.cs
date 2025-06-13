namespace ZhApi.Messages;
/// <summary>
/// 文件处理
/// </summary>
/// <param name="Title">标题</param>
/// <param name="Info"></param>
/// <param name="Current">进度值 -1 = 删除</param>
public record FileProgressMessage(string Title, XmlFileInfo Info, double Current, int Total)
    : MessageBase
{
    public string FileName => Info.XmlFile.Name;

    public string FilePath => Info.XmlFile.FullName;
}