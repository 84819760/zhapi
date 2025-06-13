namespace ZhApi.Messages;
/// <summary>
/// 红色异常
/// </summary>
/// <param name="IsRed">是否显示为红色</param>
/// <param name="IsAppend">是否添加到末尾</param>
public record class ExceptionMessage(string Message,
    bool IsRed = true, bool IsAppend = false) : MessageBase;

