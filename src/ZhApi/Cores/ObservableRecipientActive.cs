namespace ZhApi.WpfApp.Cores;
public class ObservableRecipientActive : ObservableRecipient, IDisposable
{
    private bool _disposed;
    public ObservableRecipientActive() => IsActive = true;

    public virtual void Dispose()
    {
        if (_disposed) return;
        IsActive = false;
        _disposed = true;
        OnDeactivated();
        GC.SuppressFinalize(this);
    }
}