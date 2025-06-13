namespace ZhApi.MicrosoftAI.Base;
public abstract class ChatBase<TConfig>(
    IServiceProvider service, TConfig config, ChatOptions chatOptions)
    : ChatBase(service, config, chatOptions) where TConfig : ConfigBase
{
    public TConfig Config { get; } = config;

    public static (TConfig config, ChatOptions options) CreateConfig(IConfiguration configuration)
    {
        var config = configuration.Get<TConfig>()
               ?? throw new NotImplementedException("非预期状态: 不应为空！");

        config.Test();

        var chatOptions = configuration
          .GetSection("options").Get<ChatOptions>() ?? new();

        return (config, chatOptions);
    }
}