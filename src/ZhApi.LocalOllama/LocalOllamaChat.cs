using Microsoft.Extensions.AI;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Diagnostics;
using System.Net.Http;
using ZhApi.Cores;
using ZhApi.Interfaces;
using ZhApi.Messages;

namespace ZhApi.LocalOllama;
internal class LocalOllamaChat : ChatBase<Config>, IStopModel, IDisposable,
    IModelPorvider, IStartProcess
{
    private readonly LocalOllamaService translateService;
    private readonly RequestOptions requestOptions;
    private readonly OllamaApiClient client;
    private HttpClient? hc;

    public LocalOllamaChat(IServiceProvider service, LocalOllamaService translateService,
        Config config, ChatOptions chatOptions) : base(service, config, chatOptions)
    {
        this.translateService = translateService;
        requestOptions = translateService.OllamaOptions ?? new();
        client = (CreateChatClient() as OllamaApiClient)!;
    }

    protected override string GetUrl() => Config.Url?.Trim() ?? "http://localhost:11434";

    protected override ITranslateService GetTranslateService() => translateService;

    protected override IChatClient? CreateChatClient()
    {
        try
        {
            hc = new()
            {
                BaseAddress = new(GetUrl()),
                Timeout = GetTimeout(),
            };
            return new OllamaApiClient(hc, Model);
        }
        catch (Exception ex)
        {
            FileLog.Default.LogError("CreateChatClient(): {ex}", ex.Message);
            return default;
        }
    }

    protected override async Task ReadyBody()
    {
        try
        {
            await StartProcessAsync();
            if (!Config.IsOk) return;
            await TestRequestAsync();
            await LogModelAsunc();
            Debug.Print($"{Config.Info} 就绪！");
        }
        catch (Exception ex)
        {
            new ExceptionMessage(ex.Message, IsAppend: true).SendMessage();
            throw;
        }
    }  

    private async Task TestRequestAsync()
    {
        var msgs = CreateMessages("").ToArray();
        var _ = await RequestAsync(msgs);
    }

    private async Task LogModelAsunc()
    {
        var model = Config.GetModel();
        var modelInfo = await ModelDetails.GetMethodName(httpClient, model);
        FileLog.Default.LogInformation("""      
         info           : {info}
         index          : {index}         
         model_details  : {details}
         """, Config.Info, translateService.ServiceIndex, modelInfo.Serialize(true));
    }

    private static Message ToMessage(ChatMessage mgs) => new(mgs.Role.Value, mgs.Text);

    private ChatRequest CreateChatRequest(params ChatMessage[] messages) => new()
    {
        Messages = messages.Select(ToMessage),
        Options = requestOptions,
        Stream = false,
        Think = false,
        Model = Model,
    };

    public async void StopModel()
    {
        if (!Config.IsOk) return;
        try
        {
            var chatRequest = CreateChatRequest().Set(x => x.KeepAlive = "0s");
            await client.ChatAsync(chatRequest).StreamToEndAsync();
        }
        catch (Exception) { }
    }


    protected override async Task<string> GetAsync(ChatMessage[] messages)
    {
        if (!Config.IsOk) return "";
        var chatRequest = CreateChatRequest(messages);
        var res = await client.ChatAsync(chatRequest).StreamToEndAsync();
        return res?.Message.Content ?? string.Empty;
    }

    public void Dispose()
    {
        client?.Dispose();
        httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }


    public override async Task<string[]> GetModelsAsync()
    {
        try
        {
            var models = await client.ListLocalModelsAsync();
            return models.Select(x => x.Name).ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public Task StartProcessAsync()
    {
        var exec = Config.Exec ?? new();
        return exec.TryExec();
    }
}


public class ModelTags
{
    public required ModelInfo[] Models { get; init; }

    public class ModelInfo
    {
        public required string Name { get; init; }
        public required string Model { get; init; }

    }
}
