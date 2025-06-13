namespace ZhApi.Configs;
public partial class AppConfig
{
    private const string config_file_name = "app_config.jsonc";

    static AppConfig() => SystemPromptHelper.GetSystemPrompt();

    #region Global

    public TimeSpan GetTimeout(ConfigBase config)
    {
        var timeout = config.Timeout ?? Timeout ?? 100;
        return TimeSpan.FromSeconds(timeout);
    }

    public int GetParallelism(ConfigBase config) =>
        config.Parallelism ?? Parallelism ?? 4;

    public int GetMaxLength(ConfigBase config) =>
        config.MaxLength ?? MaxLength ?? 5000;

    #endregion

    #region IsIgnores


    private string[][]? igs;
    public bool IsIgnores(string path)
    {
        const StringSplitOptions options =
            StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries;

        var items = igs ??= Ignores.Select(x => x.Split('*', options)).ToArray();
        foreach (var array in items)
            if (IsIgnores(array, path))
                return true;

        return false;
    }

    private static bool IsIgnores(string[] array, string path)
    {
        const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
        var index = 0;
        foreach (var item in array)
        {
            var i = path.IndexOf(item, index, IgnoreCase);
            if (i < 0) return false;
            index = i + item.Length;
        }
        return true;
    }

    #endregion

    public string[] GetDirectorys() =>
        Directorys.Select(Environment.ExpandEnvironmentVariables)
        .Select(x => new DirectoryInfo(x)).Where(x => x.Exists)
        .DistinctBy(x => x.FullName, IgnoreCaseEqualityComparer.Instance)
        .Select(x => x.FullName).ToArray();

    public static string GetJsonPath()
    {
        var path = Path.Combine(typeof(AppConfig).GetDirectory()
          , "zhapi", config_file_name);

        if (!File.Exists(path))
        {
            var json = Extends.SerializeNewtonsoftJson(Create());
            File.WriteAllText(path, json);
        }
        return path;
    }

  
    /// <summary>
    /// 达到修复标准
    /// </summary>
    public bool IsRepairStandards(KeyData? key) =>
        RepairCondition?.IsStandards(key) ?? false;

    public static AppConfig Create() => new()
    {
        Services =
        [ 
            // 首次翻译
            new ConfigBase()
            {
                 Model = ConfigBase.DefaultName,
                 Target = Target.All,
                 Type = "ollama",
                 Title = "待翻译",
                 Enable = true,
            },
        ],
        // 用于修复
        Repair = new[]
        {
            new ConfigBase()
            {
                 Model = ConfigBase.DefaultName,
                 Retry = new(){ Max = 5},
                 Type = "ollama",
            },
            new ConfigBase()
            {
                 Model = ConfigBase.DefaultName,
                 Retry = new(){ Max = 5},
                 Type = "ollama",
            },
        },
        Admin = true,
        Ignores =
        [
            "\\microsoft.netcore.app.ref\\ * \\System.Runtime.Intrinsics.Xml",
            //"Mono.Android.xml",
            //"Microsoft.iOS.xml",
            //"Microsoft.MacCatalyst.xml",
        ],
        Directorys =
        [
            "%ProgramFiles%\\dotnet\\packs\\",
            "%UserProFile%\\.nuget\\packages\\",
            "%ProgramFiles(x86)%\\Microsoft SDKs\\NuGetPackages\\",
        ]
    };
}
