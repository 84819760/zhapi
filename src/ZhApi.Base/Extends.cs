using Microsoft.Extensions.Logging;

namespace ZhApi;

public static partial class Extends
{
    static Extends()
    {
        RemoveElementsFlags();
        InitNewtonsoftJson();
    }

    #region SemaphoreSlim   

    public static async ValueTask RunAsync(this SemaphoreSlim slim, Action task)
    {
        await slim.WaitAsync();
        try { task(); }
        finally { slim.Release(); }
    }

    public static async ValueTask RunValueTaskAsync(this SemaphoreSlim slim, Func<ValueTask> task)
    {
        await slim.WaitAsync();
        try { await task(); }
        finally { slim.Release(); }
    }

    public static async Task RunTaskAsync(this SemaphoreSlim slim, Func<Task> task)
    {
        await slim.WaitAsync();
        try { await task(); }
        finally { slim.Release(); }
    }

    public static async Task<T> GetTaskAsync<T>(this SemaphoreSlim slim, Func<Task<T>> task)
    {
        await slim.WaitAsync();
        try { return await task(); }
        finally { slim.Release(); }
    }

    public static async ValueTask<T> GetValueAsync<T>(this SemaphoreSlim slim, Func<ValueTask<T>> task)
    {
        await slim.WaitAsync();
        try { return await task(); }
        finally { slim.Release(); }
    }
    #endregion

    public static string GetDirectory(this Type type)
    {
        var file = type.Assembly.ManifestModule.Assembly.Location;
        return Path.GetDirectoryName(file)
            ?? throw new Exception($"{file} type 加载路径为空！");
    }

    public static void Lock(this Lock @lock, Action action)
    {
        lock (@lock) action();
    }

    public static T LockValue<T>(this Lock @lock, Func<T> action)
    {
        lock (@lock) return action();
    }

    /// <summary>
    /// 延迟调用GC.Collect()
    /// </summary>
    public static async Task DelayCollectAsync(this int millisecondsDelay, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Delay(millisecondsDelay);
            GC.Collect();
        }
    }

    /// <summary>
    /// 忽略大小写
    /// </summary>
    public static bool ContainsIgnoreCase(this IEnumerable<string> item, string target)
    {
        return item.Contains(target, IgnoreCaseEqualityComparer.Instance);
    }

 

    public static T Set<T>(this T v, Action<T> action)
    {
        action(v);
        return v;
    }

    public static void TryCancel(this CancellationTokenSource cts)
    {
        try
        {
            cts.Cancel();
        }
        catch (Exception)
        { }
    }

    public static void TryDispose(this IDisposable? disposable)
    {
        if (disposable is null) return;
        try
        {
            disposable.Dispose();
        }
        catch (InvalidOperationException) { }
        catch (Exception) { }
    }

    public static void TryDispose(this Task task)
    {
        if (!task.IsCompleted) return;
        try
        {
            task.Dispose();
        }
        catch (InvalidOperationException) { }
        catch (Exception) { }
    }   
}
