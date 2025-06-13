using ZhApi.MicrosoftAI.Base;

namespace ZhApi.Services;
public class TranslateServiceInfo(ITranslateService[] translateServices)
{
    public static TranslateServiceInfo InitServices(IServiceProvider service) =>
        new(Create(service, "services", "sqlite:add"));

    public static TranslateServiceInfo InitRepair(IServiceProvider service) =>
        new(Create(service, "repair", "sqlite:update"));

    private static ITranslateService[] Create(IServiceProvider service,
        string rootName, string sqliteName)
    {
        var configuration = service.GetRequiredService<IConfiguration>();

        var root = configuration.GetSection(rootName);
        var rootType = root.GetValue<string>("type");

        var services = rootType is not null
            ? Create(service, root)
            : Create(service, root.GetChildren());

        var res = services.Append(GetService(service, sqliteName)).ToArray();
        _ = res.Aggregate((l, r) => l.SetNext(r));


        // 倒数第二个的target传递给最后一个（修复时放行）
        var last = res.Last();
        if (res.Length > 1)
        {
            var secondLast = res[^2];
            last.Config.Target = secondLast.Config.Target;
        }
        return res;
    }

    private static IEnumerable<ITranslateService> Create(
        IServiceProvider service, params IEnumerable<IConfigurationSection> sections)
    {
        var keys = service.GetRequiredService<TranslateServiceNames>().Names;

        foreach (var (index, config) in sections.Index())
        {
            var keyd = config.GetValue<string>("type")?.ToLower().Trim() 
                ?? "无效设置(type: ?)";

            if (!keys.Contains(keyd))
                yield return new ErrorService(keyd, service);
            else
                yield return GetService(service, keyd).SendConfig(config, index);
        }
    }

    private static ITranslateService GetService(IServiceProvider service, string keyd)
    {
        try
        {
            return service.GetRequiredKeyedService<ITranslateService>(keyd);
        }
        catch (InvalidOperationException ex)
        {
            var error = $"""
                找不到名为{keyd}，类型为{nameof(ITranslateService)}的服务。
                如果是自定义服务，需要注册keyd到容器中，并且实现！
                """;
            throw new InvalidOperationException(error, ex);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public ITranslateService[] Services => translateServices;  

}

public class ErrorService(string exceptionMessage, IServiceProvider service)
    : TranslateServiceBase(service)
{
    public override bool IsShowData { get; set; } = true;

    public override ConfigBase Config { get; } = new()
    {
        Title = "无效服务",
        ExceptionMessage = exceptionMessage
    };

    public override Task Completion() => Next.Completion();

    protected override Task KeyHandlerAsync(KeyData key) =>
        Next.SendAsync(key);

}