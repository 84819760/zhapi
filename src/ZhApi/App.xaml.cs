using ZhApi.Configs;
using ZhApi.Cores;
using ZhApi.SqliteDataBase;

namespace ZhApi.WpfApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public readonly static SolidColorBrush DarkRed = new(Colors.DarkRed);
    public readonly static SolidColorBrush DarkOrange = new(Colors.DarkCyan);
    public readonly static SolidColorBrush White = new(Colors.White);
    private readonly static FileLog fileLog = FileLog.Default;

    public static IServiceProvider Service { get; }

    public static Task EnsureCreatedTask { get; }

    public static MainWindowViewModel MainViewModel =>
        Service.GetRequiredService<MainWindowViewModel>();

    public static TaskbarViewModel Taskbar =>
        GetRequiredService<TaskbarViewModel>();

    public static T GetResource<T>(string key) =>
        (T)Current.Resources[key];

    public static T GetRequiredService<T>() where T : notnull =>
        Service.GetRequiredService<T>();

    static App()
    {
        Directory.CreateDirectory("zhapi\\system_prompts");

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        ErrorHandler(e.ExceptionObject as Exception, "UnhandledException");

        TaskScheduler.UnobservedTaskException += (s, e) =>
        ErrorHandler(e.Exception, "UnobservedTaskException");

        Service = new AppBuilder()
                 .Add(ShadowCodeInjectionExtensions.UseZhApi_Wpf)
                 .Build().Service;

        //Service.GetRequiredService<UserIdentity>().TryAdministrator();     
        TestAccessRun();

        EnsureCreatedTask = DataBaseInit(Service);
    }

    internal static void ErrorHandler(Exception? exception, string source)
    {
        if (exception is null) return;
        fileLog.LogError("未处理异常：{source}\r\n{ex}", source, exception.ToString());
        fileLog.Flush();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        fileLog.LogInformation("程序启动");
        Service.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DbContextBase.ClearAllPools();
        base.OnExit(e);
    }

    public static void Home()
    {
        var vm = Service.GetRequiredService<MainControlViewModel>();
        Service.GetRequiredService<MainWindowViewModel>().ControlViewModel = vm;
    }

    private static Task DataBaseInit(IServiceProvider service) => Task.Run(async () =>
    {
        var dbFactory = service.GetRequiredService<IDbContextFactory<KvDbContext>>();
        using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Database.EnsureCreatedAsync()) return;
        await db.Versions.AddAsync(new());
        await db.SaveChangesAsync();
    });

    private static void TestAccessRun()
    {
        var dirs = Service
            .GetRequiredService<IOptionsSnapshot<AppConfig>>()
            .Value.GetDirectorys();

        FileAccessHelper.TestAccessRun(dirs);
    }
}