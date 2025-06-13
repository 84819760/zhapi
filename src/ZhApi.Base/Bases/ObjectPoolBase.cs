namespace ZhApi.Bases;
public abstract class ObjectPoolBase<T> where T : class, new()
{
    private readonly static ObjectPool<T> op =
       new DefaultObjectPoolProvider().Create<T>();

    /// <summary>
    /// 返回到池中
    /// </summary>
    public abstract void ReturnPool();

    public void Return(T v) => op.Return(v);

    public static T CreateInstacne() => op.Get();
}


public class ObjectPoolHelper<T> where T : class, new()
{
    private readonly static ObjectPool<T> op =
      new DefaultObjectPoolProvider().Create<T>();

    public static T Get() => op.Get();

    public static void Return(T v) => op.Return(v);
}