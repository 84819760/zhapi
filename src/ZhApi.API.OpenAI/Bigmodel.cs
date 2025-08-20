using Newtonsoft.Json;
using System.Text.Json.Serialization;
using ZhApi.Configs;

namespace ZhApi.API.OpenAI;
[AddService<ITranslateService>(ServiceLifetime.Transient, Keyed = "bigmodel")]
public class Bigmodel(IServiceProvider service) : ChannelServiceWebApiBase(service)
{
    private BigmodelChat chat = null!;
    protected override ChatBase Chat => chat;

    protected override ConfigBase GetConfig(IConfiguration configuration)
    {
        var (config, options) = ChatBase<BigmodelConfig>.CreateConfig(configuration);
        chat = new BigmodelChat(Service, config, options, this);
        return config;
    }
}

internal class BigmodelChat(
    IServiceProvider service, BigmodelConfig config,
    ChatOptions chatOptions, ITranslateService translateService)
    : ChatClientBase<BigmodelConfig>(service, config, chatOptions, translateService)
{
    protected override string GetUrl() =>
        Config.Url ?? BigmodelConfig.defaultUrl;

    public override Task<bool> TestAsync() => Task.FromResult(true);
}

public class BigmodelConfig : Config
{
    public const string defaultUrl = "https://open.bigmodel.cn/api/paas/v4/";

    [JsonPropertyOrder(3)]
    [JsonProperty(Order = 3)]
    public override string? Url { get; set; } = defaultUrl;

    [JsonPropertyOrder(2)]
    [JsonProperty(Order = 2)]
    public override string? Model { get; set; } = "glm-4-flash";   
}