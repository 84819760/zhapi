using System.Threading.Tasks.Dataflow;
using ZhApi.Interfaces;

namespace ZhApi.Bases;
public class TimeAction : ICompletionTask
{
    private readonly BroadcastBlock<Action> broadcastBlock;
    private readonly ActionBlock<Action> actionBlock;
    private readonly int delay;

    public TimeAction(CancellationToken token, int delay = 100)
    {
        this.delay = delay;

        actionBlock = new(Handler, new()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = 1,
            EnsureOrdered = true,
            BoundedCapacity = 1,
        });

        broadcastBlock = new(x => x, new()
        {
            EnsureOrdered = true,
            CancellationToken = token
        });

        broadcastBlock.LinkTo(actionBlock);
    }

    private async Task Handler(Action action)
    {
        await Task.Delay(delay);
        action();
    }

    public async Task Completion()
    {
        broadcastBlock.Complete();
        await broadcastBlock.Completion;

        actionBlock.Complete();
        await actionBlock.Completion;
    }

    public Task SendAsync(Action action) => 
        broadcastBlock.SendAsync(action);
}