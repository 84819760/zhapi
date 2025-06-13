using System.Collections.Concurrent;

namespace ZhApi.WpfApp.LogItems;
public partial class RequestItemsViewModel : ControlProvider
{
    private readonly ConcurrentQueue<RequestItemViewModel> news = [];
    private readonly ConcurrentDictionary<Guid, bool> removes = [];
    private volatile bool isStop;

    public RequestItemsViewModel() => ForEach();

    [ObservableProperty]
    public partial int? MinWidth { get; set; }

    protected override Control CreateControl() => new RequestItems()
    {
        DataContext = this
    };

    public ObservableCollection<RequestItemViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial Visibility Visibility { get; set; } = Visibility.Collapsed;

    public void Add(RequestLength value)
    {
        var item = new RequestItemViewModel()
        {
            Gid = value.Gid,
            RequestLength = value.Length,
            RetryIndex = value.RetryId,
        };
        news.Enqueue(item);
    }

    public void Remove(Guid guid) => removes[guid] = true;

    public void Stop()
    {
        Visibility = Visibility.Collapsed;
        isStop = true;
    }

    async void ForEach()
    {
        while (!isStop)
        {
            await Task.Delay(1500);
            this.AppInvoke(Refresh);
        }
    }

    private void Refresh()
    {
        var rs = removes.Keys.ToHashSet();
        var items = Items.Concat(GetNews())
            .OrderByDescending(x => x.RequestLength)
            .ToDictionary(x => x.Gid);

        Items.Clear();

        foreach (var item in items)
        {
            var id = item.Key;
            var v = item.Value;
            if (rs.Contains(id))
            {
                removes.Remove(id, out _);
                v.Stop();
            }
            else
            {
                Items.Add(v);
            }
        }

        MinWidth = Items.Count > 0 ? 300 : 0;
    }

    private IEnumerable<RequestItemViewModel> GetNews()
    {
        while (news.TryDequeue(out var item))
            yield return item;
    }
}
