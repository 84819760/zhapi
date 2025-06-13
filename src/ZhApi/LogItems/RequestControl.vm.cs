using ZhApi.Configs;
using ZhApi.Interfaces;

namespace ZhApi.WpfApp.LogItems;
public partial class RequestControlViewModel : ControlProviderMessage
{
    protected readonly CancellationToken token;

    public RequestControlViewModel() : this(default!, default!, default!) { }

    public RequestControlViewModel(
        IOptionsSnapshot<AppConfig> appConfig,
        ITranslateService translate,
        CancellationTokenSource cts)
        : base(translate)
    {
        if (cts is null) return;
        token = cts.Token;
        ErrorWarn = new(appConfig, translate, cts);
    }

    protected override Control CreateControl() =>
        new RequestControl() { DataContext = this };

    public ErrorWarnViewModel ErrorWarn { get; set; } = null!;

    public override Task EndHandlerAsync(Exception? ex = null) =>
        ErrorWarn.EndHandlerAsync(ex);

    public override void Dispose()
    {
        base.Dispose();
        ErrorWarn?.Dispose();
        GC.SuppressFinalize(this);
    }
}