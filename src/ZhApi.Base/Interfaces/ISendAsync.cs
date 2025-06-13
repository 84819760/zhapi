namespace ZhApi.Interfaces;
public interface ISendAsync<TValue, TResult>
{
    TResult SendAsync(TValue value);
}