using OpenAI;
using Pathoschild.Http.Client;
using System.ClientModel;

namespace ZhApi.API.OpenAI;
public abstract class ChatClientBase<TConfig>(
    IServiceProvider service, TConfig config,
    ChatOptions chatOptions, ITranslateService translateService)
    : ChatBase<TConfig>(service, config, chatOptions)
    where TConfig : Config
{
    protected override ITranslateService GetTranslateService() =>
        translateService;

    protected override IChatClient? CreateChatClient()
    {
        if (!Config.IsOk) return default;
        var apikey = new ApiKeyCredential(Config.ApiKey);
        var options = new OpenAIClientOptions()
        {
            Endpoint = new(GetUrl()),
            NetworkTimeout = GetTimeout(),
        };

        return new OpenAIClient(apikey, options)
            .GetChatClient(Model)
            .AsIChatClient();
    }

    public override Task<string[]> GetModelsAsync() =>
        GetModelsAsync("/v1/models");

    protected async Task<string[]> GetModelsAsync(string path)
    {
        var apiKey = Config.ApiKey?.Trim() ?? "__";
        var res = await httpClient.GetAsync(path)
           .WithAuthentication("Bearer", apiKey)
           .As<ModelInfo>();

        return res.Data.Select(x => x.Id).ToArray();
    }
}
