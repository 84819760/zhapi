namespace ZhApi.Messages;


/// <summary>
/// 报告请求耗时
/// </summary>
public record class RequestTimeConsuming(object Target, TimeSpan TimeConsuming,
    int RequestLength, int ResponseLength,
    Guid Gid, int RetryId) : MessageTargetBase(Target);

/// <summary>
/// 报告请求重试
/// </summary>
public record class RequestRetry(object Target) : MessageTargetBase(Target);

/// <summary>
/// 报告请求不完整翻译
/// </summary>
public record class RequestWarn(object Target) : MessageTargetBase(Target);

/// <summary>
/// 报告请求请求错误
/// </summary>
public record class RequestError(object Target) : MessageTargetBase(Target);

/// <summary>
/// 请求长度
/// </summary>
public record class RequestLength(object Target, int Length, Guid Gid, int RetryId)
    : MessageTargetBase(Target);
