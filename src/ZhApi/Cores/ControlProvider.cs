using ZhApi.Cores;
using ZhApi.Interfaces;
#pragma warning disable
namespace ZhApi.WpfApp.Cores;
public abstract class ControlProvider : ObservableRecipientActive, IControlProvider
{
    private Control? control;

    public Control Control => control ??= CreateControl();

    protected abstract Control CreateControl();

    protected MainWindowViewModel MainViewModel => App.MainViewModel;

    protected FileLog FileLog => FileLog.Default;

    public virtual Task EndHandlerAsync(Exception? ex = null) =>
        Task.CompletedTask;
}

public abstract class ControlProviderMessage(ITranslateService translate) : ControlProvider
{
    private readonly static Lock @lock = new();

    protected void Lock<T>(T value, Action<T> action) where T : MessageTargetBase
    {
        if (value.Target == translate)
            lock (@lock) action(value);
    }
}