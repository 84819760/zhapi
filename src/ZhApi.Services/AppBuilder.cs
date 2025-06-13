using ZhApi.API.OpenAI;
using ZhApi.LocalOllama;
using ZhApi.MicrosoftAI.Base;
using ZhApi.DataService;

namespace ZhApi.Services;
using FuncService = Func<IServiceCollection, IConfigurationManager, IServiceCollection>;

public class AppBuilder
{
    private readonly ConfigurationManager configManager = new();
    private readonly ServiceCollection serviceCollection = new();
    private readonly List<FuncService> list = [];

    public AppBuilder()
    {
        configManager.AddNewtonsoftJsonFile(AppConfig.GetJsonPath(), true, true);
        serviceCollection.AddSingleton<IConfiguration>(configManager)
            .UseZhApi_Base(configManager)
            .UseZhApi_Services(configManager)
            .UseZhApi_LocalOllama(configManager)
            .UseZhApi_SqliteDataBase(configManager)
            .UseZhApi_API_OpenAI(configManager)
            .UseZhApi_MicrosoftAI_Base(configManager)
            .UseZhApi_DataService(configManager)
            ;
    }

    private IEnumerable<string> GetTranslateServiceKeys() => serviceCollection
        .Where(x => x.ServiceType == typeof(ITranslateService))
        .Select(x => x.ServiceKey).OfType<string>();

    public AppBuilder Add(FuncService func)
    {
        list.Add(func);
        return this;
    }

    public App Build()
    {
        foreach (var item in list)
            item(serviceCollection, configManager);

        var hs = GetTranslateServiceKeys()
            .ToHashSet(IgnoreCaseEqualityComparer.Instance);

        var translateServiceNames = new TranslateServiceNames(hs);

        serviceCollection.AddSingleton(translateServiceNames);
        return new()
        {
            Config = configManager,
            Service = serviceCollection.BuildServiceProvider()
        };
    }
}