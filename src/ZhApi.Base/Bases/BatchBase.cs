#define IsShowSimpleId1
using System.Threading.Tasks.Dataflow;
using ZhApi.Interfaces;

namespace ZhApi.Bases;

public abstract class BatchBase<T> : ICompletionTask
{
    private readonly ActionBlock<T[]> actionBlock;
    private readonly BatchBlock<T> batchBlock;
    protected readonly CancellationToken token;

    public BatchBase(int batchSize, int cacheSize = 10240, CancellationToken token = default)
    {
        this.token = token;
        batchBlock = CreateBatch(batchSize, cacheSize);
        actionBlock = CreateAction();
        batchBlock.LinkTo(actionBlock);
    }

    protected virtual ActionBlock<T[]> CreateAction()
    {
        return new(TryHandler, new()
        {
            BoundedCapacity = 1,
            MaxDegreeOfParallelism = 1,
            CancellationToken = token,
        });
    }

    protected virtual BatchBlock<T> CreateBatch(int batchSize, int cacheSize)
    {
        var cz = cacheSize > 0 ? cacheSize : batchSize * 2;
        return new(batchSize, new()
        {
            BoundedCapacity = cz,
            CancellationToken = token
        });
    }

    private async Task TryHandler(T[] values)
    {
        if (token.IsCancellationRequested) return;
        try
        {
            await Handler(values);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            throw;
        }
    }

    protected abstract Task Handler(T[] values);

    public virtual async Task Completion()
    {
        batchBlock.Complete();
        await batchBlock.Completion;

        actionBlock.Complete();
        await actionBlock.Completion;
    }

    public virtual Task<bool> SendAsync(T value) =>
        batchBlock.SendAsync(value);


#if IsShowSimpleId
    private Task ShowSimpleId(T[] value) =>
        this.ShowSimpleId(nameof(Handler), () => Handler(value));
#endif

}

public class BatchAction<T>(int batchSize,
    Func<T[], Task> handler,
    int cacheSize = 0,
    CancellationToken token = default)
    : BatchBase<T>(batchSize, cacheSize, token)
{
    protected override Task Handler(T[] values) => handler(values);
}