namespace ZhApi.Services;
[AddService(ServiceLifetime.Scoped)]
public class FileQueue(IServiceProvider service, CancellationTokenSource cts)
    : ChannelTaskBase<KeyData>(cts.Token)
{
    private readonly TranslateServiceInfo translateServices =
        TranslateServiceInfo.InitServices(service);

    public ITranslateService[] TranslateServices =>
        translateServices.Services;

    public ITranslateService TranslateService =>
        translateServices.Services[0];

    public override async Task Completion()
    {
        await base.Completion();
        await TranslateService.Completion();
    }

    protected override async Task Handler(KeyData value)
    {
        if (token.IsCancellationRequested) return;
        await TranslateService.SendAsync(value);
    }
}
