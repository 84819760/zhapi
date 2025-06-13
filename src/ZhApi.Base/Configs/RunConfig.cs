using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ZhApi.Configs;
[AddService(ServiceLifetime.Singleton)]
public partial class RunConfig : ObservableObject
{
    /// <summary>
    /// 是否覆盖zh-Hans中的文件
    /// </summary>    
    [ObservableProperty]
    public partial bool IsCover { get; set; }

    /// <summary>
    ///  停止模型，用于停止模型的运行
    /// </summary>    
    [ObservableProperty]
    public partial bool StopModel { get; set; }

    /// <summary>
    /// 完成时是否关机
    /// </summary>
    [ObservableProperty]
    public partial bool IsShutdown { get; set; }

    public void TryShutdown(Action? action)
    {
        if (!IsShutdown) return;
        try
        {
            action?.Invoke();
            Process.Start("shutdown.exe", "-s -t 60 -f");
        }
        catch (Exception ex)
        {
            FileLog.Default.LogError("调用关机失败！{ex}", ex.ToString());
        }
    }
}
