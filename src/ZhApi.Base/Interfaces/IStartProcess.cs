namespace ZhApi.Interfaces;
/// <summary>
/// 启动进程(用于启动exe)
/// </summary>
public interface IStartProcess
{
    /// <summary>
    /// <inheritdoc cref="IStartProcess"/>
    /// </summary>
    Task StartProcessAsync();
}
