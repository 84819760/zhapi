namespace ZhApi.Cores;
/// <summary>
/// 片段信息
/// </summary>
public record PathInfo(string FilePath, string MemberPath)
{
    public readonly static PathInfo ImportDataBase = new("数据库导入", "");
    public readonly static PathInfo Default = new("无路径", "导入");
}