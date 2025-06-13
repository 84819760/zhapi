using System.ComponentModel;
using ZhApi.Interfaces;

namespace ZhApi.Configs;
/// <summary>
/// 重试策略
/// </summary>
public partial class RetryStrategy : ObservableObject
{
    /// <summary>
    /// 重试最大次数(无论成功还是失败),设置为0时不重试。
    /// </summary>
    public virtual int Max { get; set; }

    /// <summary>
    /// 非失败的情况下限制相同内容次数,小于2时不做测试。
    /// </summary>
    [DefaultValue(2)]
    public virtual int Repeat { get; set; } = 2;

    /// <summary>
    /// 是否启用对话方式进行重试(能否提高重试成功率未知)
    /// </summary>
    [DefaultValue(true)]
    public bool Chat { get; set; } = true;

    /// <summary>
    /// 是否重试（用户扩展）
    /// </summary>
    /// <param name="score">最后一次响应的内容</param>
    /// <param name="index">请求的序号</param>
    public virtual bool IsRetry(IRootData data, ScoreData? score, int index, string info)
    {
        // 重试最大次数(无论成功还是失败)
        if (index >= (Max - 1)) return false;

        // 内容无效
        if (score is null) return true;

        // 完美状态不需要重试
        if (score.IsPerfect) return false;

        // 检查重复内容
        if (Repeat > 1) return IsRepeat(data, info);

        return true;
    }

    private bool IsRepeat(IRootData data, string info)
    {
        var items = data.Items.Where(x => x.GetInfo() == info)
            .GroupBy(x => x.GetRetryTest());
        return items.All(x => x.Count() < Repeat);
    }
}