using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using ZhApi.Interfaces;
namespace ZhApi.Bases;

public abstract class ChannelBase<T> : IDisposable
{
    protected readonly CancellationToken token;
    protected readonly Channel<T> channel;

    public ChannelBase(CancellationToken token, int capacity = 0)
    {
        this.token = token;
        channel = CreateChannel(capacity);
    }

    protected virtual Channel<T> CreateChannel(int capacity)
    {
        if (capacity is 0)
            return Channel.CreateUnbounded<T>();

        return Channel.CreateBounded<T>(capacity);
    }

    public virtual async Task SendAsync(T value) =>
       await channel.Writer.WriteAsync(value, token);

    protected IAsyncEnumerable<T> ReadAllAsync() =>
        channel.Reader.ReadAllAsync();

    public virtual void Dispose()
    {
        channel.Writer.TryComplete();
        _ = channel.Reader.ReadAllAsync().ToBlockingEnumerable().ToArray();
        GC.SuppressFinalize(this);
    }
}

public abstract class ChannelTaskBase<T>
    : ChannelBase<T>, ISendAsync<T, Task>, ICompletionTask
{
    private readonly Task forRunTask;

    protected ChannelTaskBase(CancellationToken token, int capacity = 0)
        : base(token, capacity) => forRunTask = Task.Run(ForRun, token);

    public virtual async Task Completion()
    {
        channel.Writer.TryComplete();
        await channel.Reader.Completion.WaitAsync(token);
        await forRunTask;
    }

    protected async Task ForRun()
    {
        var reader = channel.Reader;
        while (await reader.WaitToReadAsync(token))
        {
            var value = await reader.ReadAsync(token);
            await Handler(value);
        }
    }

    protected abstract Task Handler(T value);

    public override void Dispose()
    {
        base.Dispose();
        forRunTask.TryDispose();
        GC.SuppressFinalize(this);
    }
}


public class ChannelTimeHandler<T> : ChannelBase<T>, ICompletionTask
{
    private readonly Func<T[], Task> handler;
    private readonly TimeSpan interval;
    private volatile bool isEnd;
    private readonly Task task;

    public ChannelTimeHandler(Func<T[], Task> handler,
        int interval, CancellationToken token, int capacity = 0)
        : base(token, capacity)
    {
        this.interval = TimeSpan.FromSeconds(interval);
        this.handler = handler;
        task = Task.Run(ForTask);
    }

    private async Task ForTask()
    {
        while (!token.IsCancellationRequested && !isEnd)
            await TryAsync(HandlerAsync);

        channel.Writer.TryComplete();

        var items = channel.Reader.ReadAllAsync()
            .ToBlockingEnumerable().ToArray();

        await TryAsync(() => handler(items));
    }

    private static async Task TryAsync(Func<Task> func)
    {
        try
        {
            await func();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            FileLog.Default.LogError("{ex}", ex.ToString());
        }
    }

    private async Task HandlerAsync()
    {
        await Task.Delay(interval, token);
        var items = GetKvRows().ToArray();
        await handler(items);
    }


    private IEnumerable<T> GetKvRows()
    {
        while (channel.Reader.TryRead(out var res))
            yield return res;
    }

    public async Task Completion()
    {
        channel.Writer.TryComplete();
        isEnd = true;
        await task;
    }
}