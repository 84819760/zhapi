using ZhApi.Configs;

namespace ZhApi.Cores;
public class AutoQueue(int autoQueue, CancellationToken token)
{
    private readonly int autoQueue = autoQueue;
    private readonly SemaphoreSlim slim = new(1);
    private int limitation;

    public async Task WaitAsync(int length)
    {
        if (autoQueue is 0) return;
        await slim.WaitAsync(token);
        SpinWait.SpinUntil(() => IsOk(length));
        Interlocked.Add(ref limitation, length);
        slim.Release();
    }

    public void Release(int length) => Interlocked.Add(ref limitation, length * -1);

    private bool IsOk(int length) =>
        token.IsCancellationRequested ||
        limitation <= 0 ||
        limitation + length < autoQueue;

    public static AutoQueue Create(AppConfig appConfig, ConfigBase config, CancellationToken token)
    {
        return new AutoQueue(appConfig.GetMaxLength(config), token);
    }
}
