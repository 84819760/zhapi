using Microsoft.Extensions.Configuration;
using ZhApi.Configs;

namespace ZhApi.Interfaces;

/// <summary>
/// 翻译服务接口
/// </summary>
public interface ITranslateService : ICompletionTask
{
    /// <summary>
    /// 接受内容进行翻译（无论成功或失败都会发送到下一个服务）
    /// </summary>
    Task SendAsync(KeyData key);

    /// <summary>
    /// 设置下一个服务，返回下个服务
    /// </summary>
    ITranslateService SetNext(ITranslateService next);

    /// <summary>
    /// 设置配置
    /// </summary>
    ITranslateService SendConfig(IConfiguration config, int index);

    /// <summary>
    /// 翻译服务序号
    /// </summary>
    int ServiceIndex { get; }

    /// <summary>
    /// 是否报告请求数据(仅用于翻译服务，数据库写入不参与)
    /// </summary>
    bool IsShowData { get; }

    /// <summary>
    /// 表示服务可用
    /// </summary>
    public bool IsOk { get; }

    /// <summary>
    /// 初始化
    /// </summary>
    Task InitAsync();

    ConfigBase Config { get; }

    /// <summary>
    /// 启动服务
    /// </summary>
    void Start();
}

