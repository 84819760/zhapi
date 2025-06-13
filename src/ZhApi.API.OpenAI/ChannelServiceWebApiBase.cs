namespace ZhApi.API.OpenAI;

public abstract class ChannelServiceWebApiBase(IServiceProvider service) 
    : TranslateServiceChannel(service)
{
    // webapi 不做等待，立即执行
    public override Task InitAsync()
    {
        base.Start();
        return base.InitAsync();
    }   
}