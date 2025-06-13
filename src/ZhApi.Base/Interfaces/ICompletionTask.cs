namespace ZhApi.Interfaces;

/// <summary>
/// 执行结束的 <see cref="Task"/>
/// </summary>
public interface ICompletionTask
{
    /// <summary>
    /// <inheritdoc cref="ICompletionTask"/>
    /// </summary>
    Task Completion();
}
