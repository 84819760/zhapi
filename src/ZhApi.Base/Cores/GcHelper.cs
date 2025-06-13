#define GcTest1
using System.Diagnostics;
using ZhApi.Messages;

namespace ZhApi.Cores;
public class GcHelper
{
    protected static readonly HashSet<Func<string>> map = [];

    static GcHelper() => Test();

    private static async void Test()
    {
#if GcTest
        while (true)
        {
            await Task.Delay(5000);
            var content = map.Select(x => x()).JoinString();
            var msg = $"{content} {DateTime.Now}";
            Debug.WriteLine(msg);
            new ContentMessage(msg).SendMessage();
        }
#else 
        await Task.CompletedTask;
#endif
    }
}

public class GcHelper<T> : GcHelper
{
#if GcTest
    private static int count;

    static GcHelper() => map.Add(GetMsg);

    private static string GetMsg()
    {
        return $"{typeof(T).Name} : {count} ";
    }
#endif
    public static void Increment()
    {
#if GcTest
        Interlocked.Increment(ref count);
#endif
    }

    public static void Decrement()
    {
#if GcTest
        Interlocked.Decrement(ref count);
#endif
    }
}