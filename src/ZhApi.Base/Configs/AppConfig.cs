using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ZhApi.Interfaces;
namespace ZhApi.Configs;

/// <summary>
/// 项目配置
/// </summary>
[AddOptions]
public partial class AppConfig : ObservableObject
{  
    /// <summary>
    /// 用于翻译服务配置(扩展入口，依赖注入key = <see cref="IServiceConfig.ServiceType"/>)
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public object[]? Services { get; set; }

    /// <summary>
    /// 用于修复
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public object? Repair { get; set; }

    /// <summary>
    /// 修复标准
    /// </summary>
    public RepairCondition? RepairCondition { get; set; } = new();

    /// <summary>
    /// Sqlite设置
    /// </summary>
    [JsonProperty("sqlite")]
    public SqliteConfig? SqliteConfig { get; set; }


    #region Global  

    /// <summary>
    /// (全局)并发数量
    /// </summary>
    public int? Parallelism { get; set; } = 4;

    /// <summary>
    /// (全局)自动队列(按字符串长度进行限制)
    /// </summary>
    public int? MaxLength { get; set; } = 5000;

    /// <summary>
    /// (全局)请求超时(单位秒)
    /// </summary>
    public int? Timeout { get; set; } = 100;

    #endregion

    /// <summary>
    /// 日志文件的数量
    /// </summary>
    public int LogFileCount { get; set; } = 10;
    

    [DefaultValue(9527)]
    public bool Admin { get; set; }

    /// <summary>
    /// 检查版本的url
    /// </summary>
    public string? VersionUrl { get; set; }

    /// <summary>
    /// 完成后停止模型(默认 启用)
    /// </summary>
    [DefaultValue(true)]
    public bool CallStopModel { get; set; } = true;

    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// 包含的目录
    /// </summary>
    public string[] Directorys { get; set; } = [];

    /// <summary>
    /// 忽略文件目录，只做简单匹配(* 分割)
    /// </summary>
    public string[] Ignores { get; set; } = [];



}