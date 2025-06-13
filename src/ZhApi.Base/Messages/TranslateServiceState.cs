namespace ZhApi.Messages;
public record TranslateServiceStateMessage(object Target, TranslateServiceState State)
    : MessageTargetBase(Target);

public enum TranslateServiceState
{
    /// <summary>
    /// 服务启动
    /// </summary>
    Start,
    /// <summary>
    /// 服务初始化完成
    /// </summary>
    Ready,
    /// <summary>
    /// 最后一次调用
    /// </summary>
    End,

}