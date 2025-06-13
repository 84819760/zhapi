namespace ZhApi.Messages;

/// <summary>
/// 增加文件数量
/// </summary>
public record class IncreaseFile(int Count = 1) : MessageBase { }

/// <summary>
/// 减少文件数量
/// </summary>
public record class DecreaseFile(int Count = 1) : MessageBase { }

