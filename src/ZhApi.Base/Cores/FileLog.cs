using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using ZhApi.Bases;
using ZhApi.Configs;

namespace ZhApi.Cores;
[AddService(ServiceLifetime.Singleton, Factory = nameof(Factory))]
public class FileLog : LoggerBase
{
    public readonly static FileLog Default;
    private readonly static string logFilePath;
    private readonly StreamWriter writer;
    private readonly LogLevel level;
    private readonly FileStream fs;
    private bool _dispose;

    internal static object Factory(IServiceProvider _) => Default;

    public FileLog()
    {
        var appConfig = GetAppConfig();
        level = appConfig.LogLevel;

        fs = new(logFilePath, FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.ReadWrite);
        writer = new StreamWriter(fs) { AutoFlush = true };
    }

    static FileLog()
    {
        var path = Path.Combine("zhapi", "logs");
        var logDirectory = new DirectoryInfo(path);

        if (!logDirectory.Exists)
            logDirectory.Create();

        logFilePath = GetLogFile(logDirectory);
        File.WriteAllText(logFilePath, "");

        Default = new();
    }

    private static AppConfig GetAppConfig()
    {
        try
        {
            var json = File.ReadAllText(AppConfig.GetJsonPath());
            return json.Deserialize<AppConfig>()
                ?? throw new NotImplementedException("非预期异常");
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory("zhapi\\logs");
            File.WriteAllText($"zhapi\\logs\\_error.log", ex.ToString());
            throw;
        }
    }

    private static string GetLogFile(DirectoryInfo info)
    {
        var count = GetAppConfig().LogFileCount;
        var fileName = $"ZhApi.{DateTime.Now:yyMMdd_HH_mm_ss}.log";
        var filePath = Path.Combine(info.FullName, fileName);

        var files = info.GetFiles("ZhApi.*.log")
            .OrderBy(x => x.LastWriteTime).ToArray();

        if (files.Length < count) return filePath;

        var file = files.First();
        try { File.Delete(file.FullName); }
        catch (Exception) { }

        File.WriteAllText(filePath, "");
        return filePath;
    }


    public override async Task SendAsync(string value)
    {
        //DebugWriteLine(value);
        if (!_dispose || !token.IsCancellationRequested)
            await base.SendAsync(value);
    }

    public override bool IsEnabled(LogLevel logLevel) => logLevel >= level;

    public override void Dispose()
    {
        if (_dispose) return;
        _dispose = true;
        writer.Dispose();
        fs.Dispose();
        GC.SuppressFinalize(this);
    }

    public override async Task Completion()
    {
        await base.Completion();
        writer.Close();
        fs.Close();
    }

    protected override async Task Handler(string value)
    {
        await writer.WriteLineAsync(value);
    }

    [Conditional("DEBUG")]
    private async static void DebugWriteLine(string value)
    {
        await Task.CompletedTask;
        Debug.WriteLine(value);
    }

    public FileLog Send(LogLevel logLevel, Action<FileLog> action)
    {
        if (IsEnabled(logLevel)) action(this);
        return this;
    }


    public void Flush() => writer.Flush();

}