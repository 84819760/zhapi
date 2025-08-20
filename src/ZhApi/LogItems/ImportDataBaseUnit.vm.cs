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

        await foreach (var item in source.GetIdsAsync(sourceTab))
        {
            Current += item.Length;
            await source.SendIdsAsync(item, targetTab);
        }

        Title = "读取结束";
    });

    public Task WriteAsync(ImportBase source, ImportBase target) => Task.Run(async () =>
    {
        Current = 0;
        Total = source.DiffCount;
        Title = "写入差异";
        GC.Collect();
        using var sourceDb = await source.CreateDbContextAsync();
        var sourceMap = new Dictionary<string, long>(100);
        await foreach (var rows in source.GetDiffRows(sourceDb))
        {
            await target.SendRowsAsync(rows, sourceMap);
            Current += rows.Length;
        }

        Title = "同步完成";       
    });
}