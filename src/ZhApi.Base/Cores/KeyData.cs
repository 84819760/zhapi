using ZhApi.Configs;

namespace ZhApi.Cores;
public readonly struct KeyData
{
    public bool IsNodeLoss { get; init; }

    public bool IsWordLoss { get; init; }

    public bool IsFail { get; init; }

    public bool IsPerfect { get; init; }

    public required string Index { get; init; }

    public required double Value { get; init; }

    public bool IsDefault => !IsNodeLoss && !IsWordLoss && !IsFail && !IsPerfect;

    public bool IsTarget(Target target)
    {
        if (IsDefault) return true;
        if (IsPerfect) return false;

        return target.HasFlag(Target.All) ||
            // 失败
            (target.HasFlag(Target.Fail) && IsFail) ||
            // 节点
            (target.HasFlag(Target.Node) && IsNodeLoss) ||
            // 单词
            (target.HasFlag(Target.Word) && IsWordLoss);
    }
}