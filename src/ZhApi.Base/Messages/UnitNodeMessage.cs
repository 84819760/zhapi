namespace ZhApi.Messages;
/// <summary>
/// 报告 增加UnitNode数量
/// </summary>
public record class IncreaseUnitNode(object Target, int Count) : MessageTargetBase(Target);

/// <summary>
/// 报告 减少UnitNode数量
/// </summary>
public record class DecreaseUnitNode(object Target) : MessageTargetBase(Target);
