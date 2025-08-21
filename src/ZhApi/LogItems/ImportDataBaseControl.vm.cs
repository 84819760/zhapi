using System.Windows.Shell;
using ZhApi.Cores;
using ZhApi.SqliteDataBase;
using ZhApi.SqliteDataBase.Imports;

namespace ZhApi.WpfApp.LogItems;
public partial class ImportDataBaseControlViewModel : ControlProvider
{
    private readonly IServiceProvider service = null!;
    private readonly TaskbarViewModel taskbar = null!;
    private readonly FileLog fileLog = null!;

    protected override Control CreateControl() =>
      new ImportDataBaseControl() { DataContext = this };

    public ImportDataBaseControlViewModel() { }

    [AddService(ServiceLifetime.Scoped)]
    public ImportDataBaseControlViewModel(
        IServiceProvider service, TaskbarViewModel taskbar,
        FileLog fileLog, HomeAndStopViewModel homeStop)
    {
        this.service = service;
        this.taskbar = taskbar;
        this.fileLog = fileLog;

        HomeStop = homeStop;
    }

    [ObservableProperty]
    public partial string? Title { get; set; } = "导入初始化";

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial string? SourcePath { get; set; }

    [ObservableProperty]
    public partial Visibility AnimationVisibility { get; set; } = Visibility.Hidden;

    [ObservableProperty]
    public partial double Opacity { get; set; } = 1;

    public HomeAndStopViewModel HomeStop { get; } = null!;

    /// <summary>
    /// 读取源 写入到本地
    /// </summary>
    public ImportDataBaseUnitViewModel SourceDb { get; } = new()
    {
        Title = "初始化:源",
        Target = nameof(ImportSourceDataBase)
    };

    /// <summary>
    /// 读取本地写入到源
    /// </summary>
    public ImportDataBaseUnitViewModel LocalDb { get; } = new()
    {
        Title = "初始化:本地",
        Target = nameof(ImportLocalDataBase),
    };

    public async Task RunAsync(string dbPath)
    {
        taskbar.State = TaskbarItemProgressState.Indeterminate;
        fileLog.LogInformation("同步数据库: '{path}'", dbPath);
        SourcePath = dbPath;
        try
        {
            AnimationVisibility = Visibility.Visible;
            await Task.Run(() => ExecAsync(dbPath));
            Title = "完成";
        }
        catch (OperationCanceledException)
        {
            Title = "已取消";
        }
        catch (Exception ex)
        {
            Title = null;
            Error = ex.Message;
            fileLog.LogError("同步数据库: '{path}'\r\nException: {ex}",
                dbPath, ex.ToString());
        }
        SourceDb.IsEnd = LocalDb.IsEnd = true;
        AnimationVisibility = Visibility.Hidden;
        HomeStop.ShowHome();
        taskbar.StopIndeterminate();
    }

    private async Task ExecAsync(string dbPath)
    {
        using var local = await service
            .GetRequiredService<ImportLocalDataBase>()
            .InitAsync();

        using var source = await new ImportSourceDataBase(service, dbPath)
            .InitAsync();

        await Task.WhenAll([
          SourceDb.ReadAsync(source, local),
          LocalDb.ReadAsync(local, source),
        ]);

        await Task.WhenAll([
          SourceDb.WriteAsync(source, local),
          LocalDb.WriteAsync(local, source),
        ]);

        DbContextBase.ClearAllPools();
    }

  
}
