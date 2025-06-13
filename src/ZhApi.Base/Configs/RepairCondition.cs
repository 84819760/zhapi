namespace ZhApi.Configs;

/// <summary>
/// 修复条件
/// </summary>
public class RepairCondition
{
    private RangeData? scoreRange;

    /// <summary>
    /// 按评分筛选 表达式: [开始..结束]
    /// 大于0表示至少一个单词未翻译，
    /// 大于等于100表示节点丢失，
    /// 大于等于1000表示无中文、节点丢失、多余等问题。
    /// 格式: [开始..结束], 以下表示score大于等于5
    /// </summary>
    public string? ScoreRange { get; set; } = "[5..]";

    /// <summary>
    /// 按修复次数筛选 例如小于3次: [..3]
    /// </summary>
    public string? RepairRange { get; set; } = "[..3]";

    public RangeData GetScoreRange() => scoreRange ??= new(ScoreRange);

    /// <summary>
    /// 达到修复标准(最低分数)
    /// </summary>
    public bool IsStandards(KeyData? key)
    {
        if (!key.HasValue) return false;
        return GetScoreRange().Start >= (int)key.Value.Value;
    }
}