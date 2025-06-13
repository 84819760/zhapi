using System.Windows.Shell;
using ZhApi.Configs;
using ZhApi.Cores;
using ZhApi.Interfaces;
using ZhApi.MicrosoftAI.Base;
using ZhApi.SqliteDataBase;
using ZhApi.Sqlites;

namespace ZhApi.WpfApp;
public partial class ListControlViewModel : ControlProvider,
    IRecipient<LogMessage>, IRecipient<ExceptionMessage>,
    ISqliteUpdate, IDisposable
{
    private readonly FileCountControlViewModel? fileCountViewModel;
    private readonly RequestControlViewModel[] requestControls;
    private readonly ITranslateService[] translateServices;
    private readonly IOptionsSnapshot<AppConfig> appConfig;
    private readonly ScanService scanService = null!;
    private readonly TaskbarViewModel taskbar;

    #region ctor   
    private ListControlViewModel(IServiceProvider service, ITranslateService[] translateServices)
    {
        appConfig = service.GetRequiredService<IOptionsSnapshot<AppConfig>>();
        HomeStop = service.GetRequiredService<HomeAndStopViewModel>();
        taskbar = service.GetRequiredService<TaskbarViewModel>();
        Version = service.GetRequiredService<VersionHelper>();
        RunConfig = service.GetRequiredService<RunConfig>();

        this.translateServices = translateServices;
        requestControls = null!;
        HomeStop.Token.Register(() => ShutdownEnabled = false);
    }

    [AddService(ServiceLifetime.Scoped, Keyed = "scan")]
    public ListControlViewModel(IServiceProvider service,
        CancellationTokenSource cts, FileQueue fileQueue)
        : this(service, fileQueue.TranslateServices)
    {
        scanService = service.GetRequiredService<ScanService>();

        requestControls = CreateRequestControls(fileQueue, cts);
        Add(CreateFileProgressControls());
        Add(requestControls);
        Add(fileCountViewModel = CreateFileCountViewModel(fileQueue, cts));

    }

    [ActivatorUtilitiesConstructor]
    public ListControlViewModel(IServiceProvider service,
        CancellationTokenSource cts, ITranslateService[] translateServices)
        : this(service, translateServices)
    {
        requestControls = CreateRequestControls(translateServices, cts);
        Add(requestControls);
    }
    #endregion

    public ObservableCollection<IControlProvider> Items { get; } = [];

    public DataBaseViewModel DataBase { get; set; } = new();

    public VersionHelper Version { get; }

    public HomeAndStopViewModel HomeStop { get; }

    public RunConfig RunConfig { get; }

    [ObservableProperty]
    public partial bool ShutdownEnabled { get; set; } = true;

    protected override Control CreateControl() => new ListControl(this);

    private FileCountControlViewModel CreateFileCountViewModel(FileQueue fileQueue, CancellationTokenSource cts)
    {
        var sqliteService = fileQueue.TranslateServices.First(x => x is SqliteNewRow);
        return new FileCountControlViewModel(appConfig, sqliteService, cts);
    }

    #region IControlProvider

    private static FileProgressControlViewModel[] CreateFileProgressControls()
    {
        return
        [
            new FileProgressControlViewModel() {Title = "写入文件"},
            new FileProgressControlViewModel() {Title = "读取文件"},
        ];
    }

    private RequestControlViewModel[] CreateRequestControls(
       FileQueue fileQueue, CancellationTokenSource cts)
    {
        return CreateRequestControls(fileQueue.TranslateServices, cts);
    }

    private RequestControlViewModel[] CreateRequestControls(
       ITranslateService[] translateServices, CancellationTokenSource cts)
    {
        var array = translateServices
            .Where(x => x.IsShowData).ToArray();

        return array
             .Select(x => new RequestControlViewModel(appConfig, x, cts))
             .ToArray();
    }

    private void Add(params IEnumerable<IControlProvider> items)
    {
        foreach (var requestControl in items)
            Items.Add(requestControl);
    }


    #endregion

    #region LogControlViewModel

    private LogControlViewModel GetOrAddLogControl(bool isAppend = false)
    {
        var res = new LogControlViewModel();
        if (isAppend)
        {
            Items.Add(res);
        }
        else
        {
            var index = GetLastLogIndex();
            Items.Insert(index, res);
        }
        return res;
    }

    public void Receive(LogMessage message)
    {
        var msg = message.Message;
        this.AppBeginInvoke(() => GetOrAddLogControl().SetEnd(msg));
    }


    private int GetLastLogIndex()
    {
        var index = 0;
        var items = Items.ToArray();
        items.Reverse();
        foreach (var item in items)
        {
            if (item is LogControlViewModel) break;
            index++;
        }
        return Items.Count - index;
    }

    #endregion

    #region Exception

    private void ShowException(string message, bool isAppend, Brush background) =>
    this.AppBeginInvoke(() =>
    {
        var c = GetOrAddLogControl(isAppend);
        c.SetEnd(message);
        c.Foreground = App.White;
        c.Background = background;
    });

    // 错误
    public void Receive(ExceptionMessage message) =>
         ShowException(message.Message, message.IsAppend,
            message.IsRed ? App.DarkRed : App.DarkOrange);

    #endregion

    public async Task RunAsync(Func<Task> task)
    {
        Exception? exception = null;
        taskbar.State = TaskbarItemProgressState.Indeterminate;
        try
        {
            await translateServices.InitAsync();

            if (requestControls.Length is 0)
                throw new Exception("翻译服务列表为空");

            FileLog.LogInformation("AppConfig:\r\n{json}", appConfig.SerializeLog());

            await task();
            HomeStop.Token.ThrowIfCancellationRequested();

            Receive(new ExceptionMessage("执行结束",
                IsRed: false, IsAppend: true));
        }
        catch (OperationCanceledException)
        {
            ShowException($"任务已取消", true, App.DarkRed);
            RunConfig.IsShutdown = false;
        }
        catch (Exception ex)
        {
            FileLog.Default.LogError("{ex}", ex.ToString());
            ShowException($"{ex.Message}", true, App.DarkRed);
            RunConfig.IsShutdown = false;
        }
        finally
        {
            GC.Collect();
            ShutdownEnabled = false;
        }
        await EndHandlerAsync(exception);
        RunConfig.TryShutdown(DbContextBase.ClearAllPools);
    }

    private ITranslateService[] GetOkTranslateService() =>
        translateServices.Where(x => x.IsOk).ToArray();

    public Task SqliteUpdateAsync(SqliteUpdateService sqlite)
    {
        Receive(new LogMessage("启动修复"));
        return RunAsync(async () =>
        {
            if (GetOkTranslateService().Length is 0)
                throw new Exception("翻译服务数量为 0，请检查配置文件！");
            await sqlite.RunAsync();
        });
        throw new NotImplementedException();
    }

    public override async Task EndHandlerAsync(Exception? ex = null)
    {
        fileCountViewModel?.SetEnd();

        try
        {
            var items = Items.ToArray().OfType<ControlProvider>();
            foreach (var item in items)
                await item.EndHandlerAsync(ex);
        }
        catch (Exception)
        {

        }
        finally
        {
            HomeStop.ShowHome();
            taskbar.Set(TaskbarItemProgressState.None, 0);
        }
    }

    public Task ScanAsync(IEnumerable<XmlFileInfo> files) => RunAsync(async () =>
    {
        Receive(new LogMessage("启动文件扫描"));
        FileLog.Default.LogInformation("进入翻译");

        if (GetOkTranslateService().Length is 0)
            ShowException($"翻译服务为 0，只启用本地数据库！",
                false, App.DarkOrange);

        await scanService.ScanAsync(files);
        await scanService.Completion();
    });


    public override void Dispose()
    {
        base.Dispose();
        var items = Items.ToArray().OfType<IDisposable>();
        foreach (var item in items)
            item.TryDispose();

        GC.SuppressFinalize(this);
    }
}