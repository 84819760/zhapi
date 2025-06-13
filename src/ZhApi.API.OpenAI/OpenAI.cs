using ZhApi.Configs;

namespace ZhApi.API.OpenAI;
[AddService<ITranslateService>(ServiceLifetime.Transient, Keyed = "openai")]
[DebuggerDisplay("{GetType().Name} {GetHashCode()}")]
public class OpenAI(IServiceProvider service) : ChannelServiceWebApiBase(service)
{
    private OpenAIChatClient chat = null!;

    protected override ChatBase Chat => chat;

    protected override Task Ready => Chat.Ready;

    protected override ConfigBase GetConfig(IConfiguration configuration)
    {
        var (config, options) = ChatBase<Config>.CreateConfig(configuration);
        chat = new OpenAIChatClient(Service, config, options, this);
        return config;
    }
}

public class OpenAIChatClient(IServiceProvider service, Config config,
    ChatOptions chatOptions, ITranslateService translateService)
    : ChatClientBase<Config>(service, config, chatOptions, translateService)
{
    protected override string GetUrl() => Config.Url!;
}
