namespace ZhApi.Messages;
/// <summary>
/// 用于数据库写入和更新提示
/// </summary>
/// <param name="IsUpdate"></param>
public record DataBaseMessage(int Count, 
    bool IsUpdate = false,
    string? Target = null) : MessageBase;