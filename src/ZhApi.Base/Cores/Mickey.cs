using System.Collections.Concurrent;

namespace ZhApi.Cores;

[AddService(ServiceLifetime.Scoped)]
public class Mickey
{
    private readonly ConcurrentDictionary<string, FileData> map = [];
    private readonly static Lock _lock = new();

    public Mickey() => GcHelper<Mickey>.Increment();

    ~Mickey() => GcHelper<Mickey>.Decrement();

    public FileData this[string index] => map[index];

    public string[] Longs => map.Keys.ToArray();

    public bool Exist(string index, FileData current, out FileData file)
    {
        lock (_lock)
        {
            if (map.TryGetValue(index, out var res))
            {
                file = res;
                return true;
            }
            else
            {
                file = map[index] = current;
                return false;
            }
        }
    }
}