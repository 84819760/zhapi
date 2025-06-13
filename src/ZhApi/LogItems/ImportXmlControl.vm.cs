using System.Threading.Channels;
using ZhApi.Cores;
using ZhApi.SqliteDataBase;

namespace ZhApi.WpfApp.LogItems;
public partial class ImportXmlControlViewModel : ControlProvider
{
    private readonly Channel<XmlFileInfo> channel = null!;
    private readonly Task forTask = Task.CompletedTask;
    private readonly KvRowService kvRowService = null!;
    private readonly IServiceProvider service = null!;
    private readonly HashSet<string> fileSha256 = [];
    private readonly HashSet<string> roots = [];
    private readonly CancellationToken token;

    public ImportXmlControlViewModel() { }

    [AddService(ServiceLifetime.Scoped)]
    public ImportXmlControlViewModel(IServiceProvider service,
        HomeAndStopViewModel homeStopView,
        CancellationTokenSource cts,
        KvRowService kvRowService)
    {
        this.kvRowService = kvRowService;
        this.service = service;

        HomeStop = homeStopView;
        token = cts.Token;

        channel = Channel.CreateUnbounded<XmlFileInfo>();
        forTask = ForTask();
    }

    protected override Control CreateControl() =>
        new ImportXmlControl() { DataContext = this };

    public DataBaseViewModel DataBase { get; set; } = new();

    public HomeAndStopViewModel HomeStop { get; } = null!;

    #region 标题  
    [ObservableProperty]
    public partial string? Title { get; set; } = "初始化";

    /// <summary>
    /// 加载进度
    /// </summary>
    [ObservableProperty]
    public partial Visibility TitleVisibility { get; set; } = Visibility.Visible;

    /// <summary>
    /// 加载进度
    /// </summary>
    [ObservableProperty]
    public partial Visibility LoadingVisibility { get; set; } = Visibility.Visible;

    [ObservableProperty]
    public partial string? Error { get; set; }
    #endregion

    #region 主进度

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial int Total { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial double Current { get; set; }

    public double Progress => (Current / Total).UnNaN();

    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; }
    #endregion

    #region 文件目录名
    [ObservableProperty]
    public partial string? DirectoryName { get; set; }

    [ObservableProperty]
    public partial string? FileName { get; set; }
    #endregion

    #region 子进度

    [ObservableProperty]
    public partial Visibility ChildProgressVisibility { get; set; } = Visibility.Collapsed;

    /// <summary>
    /// 子进度
    /// </summary>
    public double ChildProgress => ChildValue / ChildTotal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChildProgress))]
    public partial int ChildTotal { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChildProgress))]
    public partial double ChildValue { get; set; }

    #endregion

    public Task RunAsync(IEnumerable<XmlFileInfo> infos, HashSet<string> roots)
    {
        foreach (var item in roots) this.roots.Add(item);
        return Task.Run(async () =>
        {
            foreach (var item in infos)
            {
                Total += 1;
                await channel.Writer.WriteAsync(item);
            }
            channel.Writer.Complete();
            await forTask;
        });
    }

    private async Task ForTask()
    {
        FileLog.LogInformation("进入同步");
        App.Taskbar.StartIndeterminate();
        TitleVisibility = LoadingVisibility = Visibility.Visible;
        try
        {
            await ExecuteAsync();
            await kvRowService.Completion();
            Title = "已完成";
        }
        catch (OperationCanceledException)
        {
            Title = "任务已取消";
        }
        catch (Exception ex)
        {
            Title = "错误";
            Error = ex.Message;
            FileLog.LogError(ex.ToString());
        }

        TitleVisibility = Visibility.Visible;
        ChildProgressVisibility = LoadingVisibility = Visibility.Collapsed;
        DirectoryName = FileName = null;
        HomeStop.ShowHome();
        App.Taskbar.StopIndeterminate();
    }

    private async Task ExecuteAsync()
    {
        IsIndeterminate = true;

        var data = new ImportXmlFileData(
          count => ChildTotal = count,
          count => ChildValue = count,
          kvRowService, null!);

        var reader = channel.Reader;
        while (await reader.WaitToReadAsync(token))
        {
            Current += 1;
            var value = await reader.ReadAsync(token);
            await Handler(value, data);
        }
    }

    private async Task Handler(XmlFileInfo info, ImportXmlFileData data)
    {
        Title = null;
        TitleVisibility = LoadingVisibility = Visibility.Collapsed;

        token.ThrowIfCancellationRequested();
        SetName(info, roots);
        try
        {
            await ImportXmlAsync(info, data);
        }
        catch (Exception ex)
        {
            FileLog.LogError("""
            file : {filePath}
            {ex}
            """, info.XmlFile.FullName, ex.Message);
        }
    }

    private void SetName(XmlFileInfo info, HashSet<string> roots)
    {
        var file = info.XmlFile;
        FileName = file.Name;
        var dir = file.Directory!.FullName;
        foreach (var item in roots)
        {
            if (dir.StartsWith(item, StringComparison.OrdinalIgnoreCase))
            {
                DirectoryName = dir.Replace(item, "").TrimStart('\\');
                return;
            }
        }
        DirectoryName = dir;
    }

    private async Task ImportXmlAsync(XmlFileInfo file, ImportXmlFileData data)
    {
        var sha256 = file.XmlFile.GetFileSha256String();
        if (!fileSha256.Add(sha256)) return;

        data = data with { Info = file };

        var memberCount = await Task.Run(() => file.AssemblyInfo.MemberCount);

        this.AppInvoke(() =>
        {
            if (memberCount < 3000)
            {
                ChildProgressVisibility = Visibility.Hidden;
                IsIndeterminate = true;
            }
            else
            {
                ChildProgressVisibility = Visibility.Visible;
                IsIndeterminate = false;
            }
        });

        await Task.Run(() =>
        {
            var import = ActivatorUtilities
            .CreateInstance<ImportXmlFile>(service, data);

            return import.RunAsync();
        });
    }
}
