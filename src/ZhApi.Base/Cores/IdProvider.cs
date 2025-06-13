namespace ZhApi.Cores;
[AddService(ServiceLifetime.Scoped)]
public class IdProvider
{
    private int _id = 0;

    public int GetId() => Interlocked.Increment(ref _id);

    public string GetIdCode() => GetId().ToString();

    public IdProvider Init(int id)
    {
        _id = id;
        return this;
    }
}