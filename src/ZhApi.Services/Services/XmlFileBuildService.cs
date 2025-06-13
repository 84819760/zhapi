#pragma warning disable CA1816
using LiteDB;

namespace ZhApi.Services;
[AddService(ServiceLifetime.Scoped)]
public class XmlFileBuildService : IDisposable
{
    private readonly List<FileData> items = [];
    private readonly static Lock _lock = new();
    private readonly IServiceProvider service;
    private readonly CancellationToken token;
    private readonly FileLog fileLog;
    private readonly Task task;
    private volatile bool isEnd;


    public XmlFileBuildService(IServiceProvider service,
        FileLog fileLog, CancellationTokenSource cts)
    {
        this.service = service;
        this.fileLog = fileLog;
        token = cts.Token;
        task = CreateTask();
        GcHelper<XmlFileBuildService>.Increment();
    }


    ~XmlFileBuildService() => GcHelper<XmlFileBuildService>.Decrement();

    internal Task SendAsync(FileData filePack)
    {
        lock (_lock) items.Add(filePack);
        return Task.CompletedTask;
    }

    private FileData[] GetCompleteds()
    {
        lock (_lock)
        {
            var ends = items.Where(x => x.IsCompleted).ToArray();
            foreach (var item in ends)
                items.Remove(item);
            return ends;
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
                return items.Count;
        }
    }

    private async Task CreateTask()
    {
        while (!IsEnd)
        {
            var ends = GetCompleteds();
            foreach (var item in ends)
                await Handler(item);

            await Task.Delay(1000, token);
        }
    }

    private bool IsEnd => token.IsCancellationRequested || (Count is 0 && isEnd);

    internal async Task Completion()
    {
        isEnd = true;
        await task;
    }

    private async Task Handler(FileData readAheadFile)
    {
        if (token.IsCancellationRequested) return;
        var builder = ActivatorUtilities
            .CreateInstance<XmlFileBuilder>(service, readAheadFile);

        var fullName = readAheadFile.XmlFileInfo.XmlFile.FullName;
        await Task.Run(builder.BuildAsync);

        fileLog.LogDebug("文件处理完成: '{file}'", fullName);

    }

    public void Dispose()
    {
        items.Clear();
        task.TryDispose();
    }
}