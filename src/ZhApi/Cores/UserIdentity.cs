//using System.Diagnostics;
//using System.Security.Principal;
//using ZhApi.Configs;

//namespace ZhApi.WpfApp.Cores;
//[AddService(ServiceLifetime.Singleton)]
//public class UserIdentity(IOptionsSnapshot<AppConfig> options)
//{
//    private static bool IsAdmin()
//    {
//        var identity = WindowsIdentity.GetCurrent();
//        var principal = new WindowsPrincipal(identity);
//        return principal.IsInRole(WindowsBuiltInRole.Administrator);

//    }

//    public void TryAdministrator()
//    {
//        if (!options.Value.Admin) return;
//        if (IsAdmin()) return;
//        try
//        {
//            TryStart();
//        }
//        catch (Exception)
//        { }
//        Environment.Exit(0);
//    }

//    private static void TryStart()
//    {
//        var exePath = Environment.CommandLine.Replace(".dll", ".exe");
//        var startInfo = new ProcessStartInfo
//        {
//            FileName = exePath,
//            Verb = "runas", // 触发 UAC 提权
//            UseShellExecute = true
//        };
//        Process.Start(startInfo);
//    }
//}
