namespace ZhApi.Configs;
[AddOptions("sqlite")]
public partial class SqliteConfig : ObservableObject
{
    public const string Default = "Data Source=zhapi\\kv.db";

    /// <summary>
    /// 连接字符串
    /// </summary>
    [ObservableProperty]
    public partial string ConnectionString { get; set; } = Default;

    /// <summary>
    /// 写入间隔(单位秒)
    /// </summary>
    [ObservableProperty]
    public partial int Interval { get; set; } = 10;

    /// <summary>
    /// 批次容量
    /// </summary>
    [ObservableProperty]
    public partial int BatchSize { get; set; } = 1000;

    /// <summary>
    /// 页面大小
    /// </summary>
    [ObservableProperty]
    public partial int PageSize { get; set; } = 3000;


}