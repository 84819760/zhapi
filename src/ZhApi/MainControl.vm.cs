using ZhApi.Configs;
using ZhApi.Cores;
using ZhApi.SqliteDataBase;

namespace ZhApi.WpfApp;
public partial class MainControlViewModel : ControlProvider
{
    private const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
    private readonly IDbContextFactory<KvDbContext> dbFactory = null!;
    private readonly IOptionsMonitor<AppConfig> appConfig = null!;
    private readonly IServiceProvider service = null!;

    protected override Control CreateControl() => new MainControl(this);

    public MainControlViewModel() { }


    [AddService(ServiceLifetime.Singleton)]
    public MainControlViewModel(IServiceProvider service,
        IDbContextFactory<KvDbContext> dbFactory,
        IOptionsMonitor<AppConfig> appConfig,
        VersionHelper version,
        RunConfig runConfig)
    {
        this.service = service;
        this.dbFactory = dbFactory;
        this.appConfig = appConfig;

        Version = version;
        RunConfig = runConfig;
    }

    #region Prop   
    [ObservableProperty]
    public partial Visibility ButtonsVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public VersionHelper Version { get; } = null!;

    public RunConfig RunConfig { get; } = null!;
    #endregion

    #region GetFiles
    private static IEnumerable<string> GetFiles(FileSystemInfo info, string extension)
    {
        const SearchOption search = SearchOption.AllDirectories;

        if (info is FileInfo file && file.Extension.Equals(extension, ignoreCase))
            return [info.FullName];

        if (info is DirectoryInfo dir)
            return dir.EnumerateFiles($"*{extension}", search).Select(x => x.FullName);

        return [];
    }

    private static IEnumerable<string> GetFiles(string[] paths, string extension) =>
        paths.Select(GetFileInfos).SelectMany(x => GetFiles(x, extension));

    private static FileSystemInfo GetFileInfos(string path)
    {
        if (File.Exists(path)) return new FileInfo(path);
        else if (Directory.Exists(path)) return new DirectoryInfo(path);
        throw new NotImplementedException($"非预期路径:{path}");
    }
    #endregion

    #region 翻译

    private IEnumerable<XmlFileInfo> GetXmlFileInfos(string[] paths) =>
        GetFiles(paths, ".xml").Where(x => !appConfig.CurrentValue.IsIgnores(x))
        .Where(x => !IsZhHhans(x)).Select(x => new XmlFileInfo(x));

    private static bool IsZhHhans(string path)
    {
        var dir = Path.GetDirectoryName(path);
        return dir?.EndsWith("\\zh-hans", ignoreCase) ?? false;
    }

    [RelayCommand]
    public async Task TranslateClick()
    {
        IsEnabled = false;
        var dirs = appConfig.CurrentValue.GetDirectorys();
        var files = GetXmlFileInfos(dirs).Where(IsTarget);
        await TranslateDrop(files);
    }

    private bool IsTarget(XmlFileInfo info) =>
        (RunConfig.IsCover || !info.ZhHans.Exists) && info.Dll.Exists;

    [RelayCommand]
    public async Task TranslateDrop(DragEventArgs e)
    {
        IsEnabled = false;
        var data = e.Data.GetData(DataFormats.FileDrop);
        if (data is not string[] paths) return;
        var files = paths.SelectMany(GetDropXmlFileInfos).ToArray();
        await TranslateDrop(files);
    }

    private IEnumerable<XmlFileInfo> GetDropXmlFileInfos(string path)
    {
        if (File.Exists(path))
        {
            var helper = new PackHelper(path);
            if (helper.IsTargetFile())
                return GetXmlFileInfos([.. helper.GetPackDirectorys()])
                    .Where(IsTarget);

            if (helper.Extension is ".xml" && !IsZhHhans(path))
                return [new(path)];
        }
        else if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories)
                .Where(x => !IsZhHhans(x))
                .Select(x => new XmlFileInfo(x));
        }
        return [];
    }

    private async Task TranslateDrop(IEnumerable<XmlFileInfo> paths)
    {
        using var s = service.CreateScope();
        var sp = s.ServiceProvider;
        var logList = sp.GetRequiredKeyedService<ListControlViewModel>("scan");
        MainViewModel.ControlViewModel = logList;
        await Task.Run(() => logList.ScanAsync(paths));
        IsEnabled = true;
    }

    #endregion

    #region 同步数据库

    [RelayCommand]
    public async Task SyncDataBaseClick()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            FileName = "kv.db",
            DefaultExt = ".db",
            Filter = "数据库文件 (.db)|*.db"
        };
        var result = dialog.ShowDialog() ?? false;
        if (!result) return;

        var file = dialog.FileName;
        await SyncDataBaseAsync(file);
    }

    [RelayCommand]
    public async Task SyncDataBaseDrop(DragEventArgs e)
    {
        var data = e.Data.GetData(DataFormats.FileDrop);
        if (data is not string[] paths) return;

        var file = await GetDatabaseFile(paths);
        if (file == null) return;
        await SyncDataBaseAsync(file);
    }

    private static Task<string?> GetDatabaseFile(string[] paths) => Task.Run(() =>
    {
        var files = GetFiles(paths, ".db").ToArray();
        return files.FirstOrDefault(x => x.EndsWith("kv.db", ignoreCase)) ??
              files.FirstOrDefault(x => x.EndsWith(".db", ignoreCase));
    });

    private async Task SyncDataBaseAsync(string file)
    {
        IsEnabled = false;
        using var scop = this.service.CreateScope();
        var service = scop.ServiceProvider;
        var vm = service.GetRequiredService<ImportDataBaseControlViewModel>();
        this.AppInvoke(() => App.MainViewModel.ControlViewModel = vm);
        await vm.RunAsync(file);
        IsEnabled = true;
    }

    #endregion

    #region 修复

    [ObservableProperty]
    public partial string? RepairTitle { get; set; }

    public async Task SetRepairCountAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var rc = appConfig.CurrentValue.RepairCondition;
        var count = await SqliteUpdateService.Queryable(db, rc).CountAsync();
        RepairTitle = $"[{count}]";
    }

    [RelayCommand]
    public async Task ResetRepair()
    {
        IsEnabled = false;
        await Task.Run(UpdateRepairCountAsync);
        IsEnabled = true;
    }

    private async Task UpdateRepairCountAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        await db.KvRows.Where(x => x.Score > 0)
            .ExecuteUpdateAsync(p => p
            .SetProperty(x => x.SourceId, x => Math.Abs(x.SourceId))
            .SetProperty(x => x.RepairCount, x => 0));

        await SetRepairCountAsync();
    }

    [RelayCommand]
    public async Task RepairClick()
    {
        IsEnabled = false;
        FileLog.LogInformation("进入修复");
        using var scope = this.service.CreateScope();
        var service = scope.ServiceProvider;
        var info = TranslateServiceInfo.InitRepair(service);

        var vm = ActivatorUtilities
            .CreateInstance<ListControlViewModel>(service, [info.Services]);

        var sqliteUpdateService = ActivatorUtilities
           .CreateInstance<SqliteUpdateService>(service, info);

        MainViewModel.ControlViewModel = vm;
        await Task.Run(() => ((ISqliteUpdate)vm).SqliteUpdateAsync(sqliteUpdateService));

        IsEnabled = true;
    }

    #endregion

    #region 导入xml
    private static IEnumerable<XmlFileInfo> GetXmls(string[] paths) =>
        GetFiles(paths, ".xml")
        .Select(x => new XmlFileInfo(x))
        .Where(x => x.ZhHans.Exists);

    [RelayCommand]
    public async Task ImportXmlClick()
    {
        var dirs = appConfig.CurrentValue.GetDirectorys();
        var roots = dirs.ToHashSet();
        await ImportXmlPaths(dirs, roots);
    }

    [RelayCommand]
    public async Task ImportXmlDrop(DragEventArgs e)
    {
        var data = e.Data.GetData(DataFormats.FileDrop);
        if (data is not string[] paths) return;
        await ImportXmlPaths(paths, []);
    }

    private async Task ImportXmlPaths(string[] paths, HashSet<string> roots)
    {
        IsEnabled = false;
        using var scop = this.service.CreateScope();
        var service = scop.ServiceProvider;
        var vm = service.GetRequiredService<ImportXmlControlViewModel>();
        App.MainViewModel.ControlViewModel = vm;
        var infos = GetXmls(paths);
        await vm.RunAsync(infos, roots);
        IsEnabled = true;
    }
    #endregion
}