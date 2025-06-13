namespace ZhApi.Interfaces;

/// <summary>
/// 非重复添加
/// </summary>
public interface ITryAdds<T>
{
    /// <summary>
    /// 写入行通知
    /// </summary>
    Action<int>? NotifySaveRowCount { get; set; }

    Task<int> TryAdds(T[] values);
}