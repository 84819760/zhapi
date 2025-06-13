using Microsoft.Extensions.Options;
using Pathoschild.Http.Client;
namespace ZhApi.MicrosoftAI.Base;

public abstract class ChatBase : IModelPorvider, IEffectiveTesting
{
    protected readonly IServiceProvider service;
    protected readonly FluentClient httpClient;

    private readonly ChatMessage? systemMessage;
    private readonly CancellationToken token;
    private readonly ChatOptions chatOptions;
    private readonly IChatClient? chatClient;
    private readonly IdProvider idProvider;
    private readonly AppConfig appConfig;
    private readonly AutoQueue autoQueue;
    private readonly ConfigBase config;


    private readonly Lazy<ITranslateService> translateService;
    private readonly Lazy<Task<bool>> testLazy;
    private readonly Lazy<ModelInfo> infoLazy;
    private readonly Lazy<Task> readyLazy;

    public ChatBase(IServiceProvider service, ConfigBase config, ChatOptions chatOptions)
    {
        this.service = service;
        this.config = config;
        this.chatOptions = chatOptions;

        service.GetRequiredService<ScopedConfig>().Add(this);

        appConfig = service.GetRequiredService<IOptionsSnapshot<AppConfig>>().Value;
        token = service.GetRequiredService<CancellationTokenSource>().Token;
        DataService = service.GetRequiredService<IDataService>();
        idProvider = service.GetRequiredService<IdProvider>();

        Model = config.GetModel();

        systemMessage = CreateSystemMessage();
        httpClient = new(GetUrl());
        readyLazy = new(ReadyTask);

        translateService = new(GetTranslateService);
        infoLazy = new(config.GetModelInfo);
        testLazy = new(CreateTest);
        chatClient = CreateChatClient();

        autoQueue = AutoQueue.Create(appConfig, config, token);
    }

    public string Model { get; }

    public ModelInfo Info => infoLazy.Value;

    protected abstract ITranslateService GetTranslateService();

    protected abstract IChatClient? CreateChatClient();

    public abstract Task<string[]> GetModelsAsync();

    public IDataService DataService { get; }

    public ITranslateService TranslateService => translateService.Value;

    public Task Ready => readyLazy.Value;

    protected abstract string GetUrl();

    protected TimeSpan GetTimeout() => appConfig.GetTimeout(config);

    public virtual ChatMessage? CreateSystemMessage()
    {
        if (!config.IsOk) return null;
        var txt = config.GetSystemPrompt()
            ?? SystemPromptHelper.GetSystemPrompt(Model);

        if (txt is not { Length: > 0 }) return null;
        return new(ChatRole.System, txt);
    }

    private async Task ReadyTask()
    {
        var ts = TranslateService;

        new TranslateServiceStateMessage(ts, TranslateServiceState.Start)
            .SendMessage();
        if (config.IsOk)
        {
            FileLog.Default.LogInformation("""
             ℹ️ System Prompt 
             {info}
             {sp}
             """, config.Info, systemMessage?.Text);
        }

        if (this is IEffectiveTesting test)
            await test.TestAsync();

        await ReadyBody();
        new TranslateServiceStateMessage(ts, TranslateServiceState.Ready)
            .SendMessage();

        if (!config.IsOk)
        new TranslateServiceStateMessage(ts, TranslateServiceState.End)
                .SendMessage();
    }

    protected virtual Task ReadyBody() => Task.CompletedTask;

    protected virtual async Task<string> GetAsync(ChatMessage[] messages)
    {
        if (chatClient is null) return string.Empty;

        var req = chatClient
            .GetResponseAsync(messages, chatOptions, token);

        return (await req).Text;
    }

    protected async Task<ResponseData> RequestAsync(ChatMessage[] messages)
    {
        var index = idProvider.GetId();
        var res = ResponseData.Create(messages, Info, index);
        var start = DateTime.Now;
        try
        {
            var content = await GetAsync(messages).WaitAsync(token);
            res.Response = RepairXml.Repair(content);
        }
        catch (Exception ex)
        {
            res.Response = "".GetXmlMarkdown();
            res.IsTimeout = ex is TaskCanceledException;
            res.Exception = ex.Message;
        }
        res.Time = DateTime.Now - start;
        return res;
    }

    protected IEnumerable<ChatMessage> CreateMessages(string content, ScoreData? score = null)
    {
        if (systemMessage is not null)
            yield return systemMessage;

        yield return new(ChatRole.User, content.GetXmlMarkdown());

        if (score is null || score.GetInfo() != Info.Info)
            yield break;

        var last = score.Xml.GetXmlMarkdown();
        yield return new(ChatRole.Assistant, last);

        var prompt = score.GetRetryMessage();
        yield return new(ChatRole.User, prompt);
    }   

    private async Task<ResponseData> RequestAsync(string request, int retryIndex, ScoreData? score)
    {
        var id = Guid.NewGuid();
        var requestLength = request.Length;

        new RequestLength(TranslateService, request.Length, id, retryIndex)
          .SendMessage();

        var messages = CreateMessages(request, score).ToArray();
        var res = await RequestAsync(messages);

        new RequestTimeConsuming(TranslateService, res.Time,
            requestLength, res.Response.Length,
            id, retryIndex).SendMessage();

        return res;
    }

    public async Task<ResponseData> RequestAsync(IRootData data, int retryIndex)
    {
        var request = data.OriginalXml;
        var requestLength = request.Length;
        await autoQueue.WaitAsync(requestLength);
        var res = await RequestAsync(request, retryIndex, data.Items.LastOrDefault());
        autoQueue.Release(requestLength);
        Log(res, request);
        return res;
    }

    private static void Log(ResponseData data, string request)
    {
        if (data.Messages.Length > 2)
        {
            FileLog.Default.LogDebug("chat 重试\r\n{data}", data);
        }
        else if (data.Time.TotalSeconds > 10)
        {
            FileLog.Default.LogDebug("""
            时间过长! 
            耗时: {time}
            请求: {reqLength}
            {req}
            响应: {resLength}
            {res}
            """,
            data.Time,
            request.Length,
            request,

            data.Response.Length,
            data.Response);
        }
    }

    #region Test
    async Task StartProcessAsync()
    {
        if (this is IStartProcess chat)
            await chat.StartProcessAsync();
    }

    async Task TestModelNameAsync()
    {
        if (this is not IModelPorvider chat) return;

        var model = config.GetModel();
        var models = await chat.GetModelsAsync();

        if (!IsContainModel(model, models))
            config.ExceptionMessage = $"'{model}' 不在模型列表中!";
    }

    static bool IsContainModel(string model, IEnumerable<string> strings)
    {
        var hs = strings.ToHashSet(IgnoreCaseEqualityComparer.Instance);
        return new[] { model, $"{model}:latest" }.Any(hs.Contains);
    }

    private async Task<bool> CreateTest()
    {
        new ServiceTestMessage(TranslateService, "< 测试").SendMessage();

        // 启动进程
        await StartProcessAsync();

        // 模型列表
        await TestModelNameAsync();

        new ServiceTestMessage(TranslateService, " .").SendMessage();
        return config.IsOk;
    }
    #endregion

    public Task<bool> TestAsync() => testLazy.Value;
}
