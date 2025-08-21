using ZhApi.SqliteDataBase.Imports;

namespace ZhApi.WpfApp.LogItems;
public partial class ImportDataBaseUnitViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial int Total { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial double Current { get; set; }

    [ObservableProperty]
    public partial bool IsEnd { get; set; }

    public double Progress => (Current / Total).UnNaN();

    public required string Target
    {
        get => DataBase.Target;
        set => DataBase.Target = value;
    }

    public DataBaseUnitViewModel DataBase { get; set; } = new()
    {
        Visibility = Visibility.Visible
    };


    public Task ReadAsync(ImportBase source, ImportBase target) => Task.Run(async () =>
    {
        Current = 0;
        Total = source.Count;
        Title = "读取差异";

        using var sourceDb = await source.CreateDbContextAsync();
        using var targetDb = await target.CreateDbContextAsync();

        var sourceTab = sourceDb.KvRows.AsNoTracking();
        var targetTab = targetDb.KvRows.AsNoTracking();

        await foreach (var rows in source.GetIdsAsync(sourceTab))
        {
            var ids = await new ReadIdHelper(rows).GetIdsAsync(targetTab);
            source.SendIds(ids);
            Current += rows.Length;
        }

        Title = "读取结束";
    });

    public Task WriteAsync(ImportBase source, ImportBase target) => Task.Run(async () =>
    {
        const string title = "写入";
        Current = 0;
        Total = source.DiffCount;
        Title = title;
        using var sourceDb = await source.CreateDbContextAsync();
        var sourceMap = new Dictionary<string, long>(100);
        await foreach (var rows in source.GetKvRowSources(sourceDb))
        {
            var diff = await target.SaveRowsAsync(rows, sourceMap);
            Current += rows.Count;
            Title = $"{title} {diff}";
        }

        Title = "同步完成";
    });
}