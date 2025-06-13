using OllamaSharp.Models;
using ZhApi.Interfaces;

namespace ZhApi.LocalOllama;
[AddService<ITranslateService>(ServiceLifetime.Transient, Keyed = "ollama")]
internal class LocalOllamaService(IServiceProvider service)
    : TranslateServiceChannel(service), IStopModel
{
    private LocalOllamaChat chatBase = null!;

    public RequestOptions OllamaOptions { get; private set; } = null!;

    protected override Task Ready => chatBase.Ready;

    protected override ConfigBase GetConfig(IConfiguration configuration)
    {
        var (config, options) = ChatBase<Config>.CreateConfig(configuration);
        chatBase = new LocalOllamaChat(Service, this, config, options);

        OllamaOptions = configuration
           .GetSection("options")
           .Get<RequestOptions>() ?? new();

        return config;
    }

    public void StopModel() => chatBase?.StopModel();

    protected override ChatBase Chat => chatBase;
}
