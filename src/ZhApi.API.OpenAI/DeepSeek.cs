using Newtonsoft.Json;
using System.Text.Json.Serialization;
using ZhApi.Configs;

namespace ZhApi.API.OpenAI;
[AddService<ITranslateService>(ServiceLifetime.Transient, Keyed = "deepseek")]
[DebuggerDisplay("{GetType().Name} {GetHashCode()}")]
public class DeepSeekService(IServiceProvider service) : ChannelServiceWebApiBase(service)
{
    private DeepSeekChatClient chat = null!;

    protected override ChatBase Chat => chat;

    protected override Task Ready => Chat.Ready;

    protected override ConfigBase GetConfig(IConfiguration configuration)
    {
        var (config, options) = ChatBase<DeepSeekConfig>.CreateConfig(configuration);
        chat = new DeepSeekChatClient(Service, config, options, this);
        return config;
    }
}

public class DeepSeekChatClient(
    IServiceProvider service, DeepSeekConfig config,
    ChatOptions chatOptions, ITranslateService translateService)
    : ChatClientBase<DeepSeekConfig>(service, config, chatOptions, translateService)
{
    protected override string GetUrl() =>
        Config.Url ?? DeepSeekConfig.defaultUrl;
}

public class DeepSeekConfig : Config
{
    public const string defaultUrl = "https://api.deepseek.com";

    [JsonPropertyOrder(3)]
    [JsonProperty(Order = 3)]
    public override string? Url { get; set; } = defaultUrl;

    [JsonPropertyOrder(2)]
    [JsonProperty(Order = 2)]
    public override string? Model { get; set; } = "deepseek-chat";
}