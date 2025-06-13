using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZhApi.Cores;
using ZhApi.Interfaces;

namespace ZhApi.LocalOllama;
public class Config : ConfigBase, IServiceConfig
{
    [JsonPropertyOrder(0)]
    [JsonProperty(Order = 0)]
    public override string Type { get; set; } = "ollama";

    [JsonPropertyOrder(3)]
    [JsonProperty(Order = 3)]
    public override string? Url { get; set; } = "http://localhost:11434";


    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public override bool IsOk => base.IsOk && Model is { Length: > 0 } && Model is not DefaultName;

    /// <summary>
    /// 启动exe相关设置
    /// </summary>
    public ExecInfo? Exec { get; set; }

}

public class ExecInfo
{
    private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
    private const string ollamaPath = "%LOCALAPPDATA%\\Programs\\Ollama\\ollama app.exe";
    private static readonly SemaphoreSlim slim = new(1);

    /// <summary>
    /// 监控的进程名称
    /// </summary>
    public string? ProcessName { get; init; } = "ollama app";

    /// <summary>
    /// 监控进程对应的exe路径
    /// </summary>
    public string? ExePath { get; init; } = ollamaPath;

    /// <summary>
    /// 调用路径(可能是批处理，因此和ExePath可能不同)
    /// </summary>
    public string? CallPath { get; init; } = ollamaPath;


    public Task TryExec() => Task.Run(CallExec);

    private async Task CallExec()
    {
        await slim.WaitAsync();
        if (ExePath is null || ProcessName is null || CallPath is null) return;
        try
        {
            // 监控进程对应的exe路径
            var path = Environment.ExpandEnvironmentVariables(ExePath);

            var process = GetProcess(ProcessName, path);
            if (process != null) return;

            // 调用bat或exe,用于启动监控exe
            var call = Environment.ExpandEnvironmentVariables(CallPath);
            Process.Start(call);

            // 等待进程就绪(10秒)
            for (int i = 0; i < 10; i++)
            {
                var p = GetProcess(ProcessName, path);
                if (p != null) return;
                await Task.Delay(1000);
            }
            throw new Exception($"调用{call},后查找进程{path}失败！");

        }
        catch (Exception ex)
        {
            FileLog.Default.LogCritical("""
                启动进程失败！
                进程名称 : '{ProcessName}' 
                监控路径 : '{ExePath}'
                启动路径 : '{CallPath}'
                {ex}
                """, ProcessName, ExePath, CallPath, ex.ToString());
        }
        finally
        {
            slim.Release();
        }
    }

    private static Process? GetProcess(string processName, string exePath)
    {
        // 通过进程名称获取exe路径与监控exe路径比较
        return Process.GetProcesses()
              .Where(x => x.ProcessName.Equals(processName, IgnoreCase))
              .FirstOrDefault(x => x.MainModule?.FileName == exePath);
    }
}