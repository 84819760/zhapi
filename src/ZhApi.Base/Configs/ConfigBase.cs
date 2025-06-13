using Newtonsoft.Json;
using System.ComponentModel;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZhApi.Interfaces;

namespace ZhApi.Configs;
public partial class ConfigBase : ObservableObject
{
    public const string DefaultName = "未设置";

    /// <summary>
    /// 服务类型
    /// </summary>
    [JsonPropertyOrder(0)]
    [JsonProperty(Order = 0)]
    public virtual string Type { get; set; } = string.Empty;

    /// <summary>
    /// 服务名称
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonProperty(Order = 1)]
    public virtual string? Title { get; set; }

    /// <summary>
    /// 模型
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonProperty(Order = 2)]
    public virtual string? Model { get; set; }

    /// <summary>
    /// 服务地址
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonProperty(Order = 3)]
    public virtual string? Url { get; set; }


    /// <summary>
    /// 要处理的类型（单词，节点丢失，翻译失败）
    /// </summary>
    [DefaultValue(Target.All)]
    [JsonPropertyOrder(5)]
    [JsonProperty(Order = 5)]
    public virtual Target Target { get; set; } = Target.All;

    #region 单独设置   

    /// <summary>
    /// 并发数量（本地服务默认值:4）
    /// </summary>
    [JsonPropertyOrder(6)]
    [JsonProperty(Order = 6)]
    public virtual int? Parallelism { get; set; }

    /// <summary>
    /// 按字符串长度总数拆分队列(每个服务可单独设置)，单个字符串超出长度的节点会被忽略。
    /// <br/>Ollama默认值5000，DeepSeek和OpenAI默认值0。设置为0时停用。
    /// <br/>用于解决并发时多个超大字符串进入模型导致的超时。可根据显卡性能适当调整。
    /// </summary>
    [JsonPropertyOrder(6)]
    [JsonProperty(Order = 6)]
    public virtual int? MaxLength { get; set; }

    /// <summary>
    /// 请求超时(单位秒)
    /// </summary>
    [JsonPropertyOrder(6)]
    [JsonProperty(Order = 6)]
    public virtual int? Timeout { get; set; }

    #endregion

    /// <summary>
    /// 重试策略
    /// </summary>
    [JsonPropertyOrder(7)]
    [JsonProperty(Order = 7)]
    public virtual RetryStrategy? Retry { get; set; }

    public RetryStrategy GetRetry() => Retry ??= new();

    /// <summary>
    /// 模型配置
    /// </summary>
    [JsonPropertyOrder(8)]
    [JsonProperty(Order = 8)]
    public virtual Dictionary<string, object>? Options { get; set; }


    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyOrder(9000)]
    [JsonProperty(Order = 9000)]
    [DefaultValue(9527)]
    public virtual bool Enable { get; set; }

    [JsonPropertyOrder(9527)]
    [Newtonsoft.Json.JsonIgnore]
    public virtual bool IsOk => Enable && ExceptionMessage is null && Type is { Length: > 0 };


    [JsonPropertyOrder(9527)]
    [Newtonsoft.Json.JsonIgnore]
    public string Info => info ??= $"[{Index} | {Type} | {GetModelValue() ?? Title}]";
    private string? info;

    [JsonPropertyOrder(9527)]
    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    public partial string? ExceptionMessage { get; set; }


    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int Index { get; set; } = 9527;

}