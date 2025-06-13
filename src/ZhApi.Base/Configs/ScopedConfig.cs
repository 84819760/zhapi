using ZhApi.Interfaces;

namespace ZhApi.Configs;

/// <summary>
/// 范围内集合
/// </summary>
[AddService(ServiceLifetime.Scoped)]
public class ScopedConfig
{
    private readonly HashSet<IEffectiveTesting> testings = [];
    private readonly Lazy<Task<int>> lazy;

    public ScopedConfig() => lazy = new(GetEffectiveCountAsync);

    public Task<int> CountAsync() => lazy.Value;

    private Task<int> GetEffectiveCountAsync() => Task.Run(() =>
    testings.AsParallel()
    .Select(async x => await x.TestAsync())
    .Where(x => x.Result).Count());

    public void Add<T>(T value)
    {
        if (value is IEffectiveTesting et) testings.Add(et);
    }
}
