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

    public Task RunAsync(ImportBase source, ImportBase target) =>
    Task.Run(async () =>
    {
        Total = source.Count;
        await foreach (var item in source.GetRowsAsync())
        {
            Current += item.Length;
            await target.SendAsync(item);
        }
        await target.Completion();
    });
}