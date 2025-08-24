using System.Xml.Linq;

namespace ZhApi;
public class PackHelper
{
    private static readonly string nugetPath = Environment.ExpandEnvironmentVariables(NUGET_PACKAGES);
    private const string NUGET_PACKAGES = "%userprofile%\\.nuget\\packages";
    private const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
    private readonly Dictionary<string, Func<string, IEnumerable<string>>> map;
    private readonly string file;
    public PackHelper(string file)
    {
        this.file = file;
        Extension = Path.GetExtension(file).ToLower();
        map = new()
        {
            {".sln", SlnHandler },
            {".slnx", SlnxHandler },
            {".csproj", CsprojHandler }
        };
    }

    public string Extension { get; }

    /// <summary>
    /// 是否为支持的文件扩展名
    /// </summary>
    public bool IsTargetFile() => map.ContainsKey(Extension);

    public IEnumerable<string> GetPackDirectorys() => GetCsprojs()
        .SelectMany(file => GetPaths(file, "PackageReference", "Include", nugetPath))
        .Distinct().Where(Directory.Exists);

    public IEnumerable<string> GetCsprojs()
    {
        if (map.TryGetValue(Extension, out var handler))
            return handler(file).Distinct();
        return [];
    }

    #region path

    private static string GetBasePath(string path) =>
        Path.GetDirectoryName(path) ?? throw new NotImplementedException($"路径不正确:{path}");

    private static string GetFullPath(string path, string basePath)
    {
        if (path[1] == ':') return path.Replace("/", "\\");
        return Path.GetFullPath(path, basePath);
    }

    private IEnumerable<string> GetProjectReferences(string file) =>
       GetPaths(file, "ProjectReference", "Include")
       .SelectMany(GetProjectReferences).Prepend(file);

    #endregion

    #region xml

    private static string? GetAttribute(XElement e, string name) =>
       e.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, ignoreCase))?.Value;

    private static IEnumerable<string> GetPaths(string path, string elementName, string attributeName, string? basePath = null)
    {
        if (!File.Exists(path)) return [];
        basePath ??= GetBasePath(path);
        var xmldoc = XDocument.Load(path);
        return xmldoc.DescendantNodes().OfType<XElement>()
              .Where(x => x.Name.LocalName.Equals(elementName, ignoreCase))
              .Select(x => GetAttribute(x, attributeName))
              .OfType<string>()
              .Select(x => GetFullPath(x, basePath));
    }

    #endregion

    #region sln

    private IEnumerable<string> SlnxHandler(string path) =>
       GetPaths(path, "Project", "Path").SelectMany(GetProjectReferences);

    private IEnumerable<string> SlnHandler(string file) =>
        GetSlnCsprojs(file).SelectMany(GetProjectReferences);

    private static IEnumerable<string> GetSlnCsprojs(string file)
    {
        var basePath = GetBasePath(file);
        var content = File.ReadAllText(file);
        foreach (var item in Search(content, "Project", "EndProject"))
        {
            var path = item.Split(',').ElementAtOrDefault(1) ?? "";
            path = path.Trim(['\"', ' ']).Trim();
            if (!path.EndsWith(".csproj", ignoreCase)) continue;

            var fullPath = Path.GetFullPath(path, basePath);
            if (File.Exists(fullPath)) yield return fullPath;
        }
    }

    private static List<string> Search(string content,
        string start = "Project", string end = "EndProject")
    {
        var span = content.AsSpan();
        var res = new List<string>();

        while (true)
        {
            var s = span.IndexOf(start, ignoreCase);
            if (s is -1) break;
            s += start.Length;

            var e = span.IndexOf(end, ignoreCase);
            if (e is -1) break;

            res.Add(span[s..e].ToString());
            span = span[(e + end.Length)..];
        }
        return res;
    }

    #endregion

    #region csproj  

    private IEnumerable<string> CsprojHandler(string file) =>
        GetProjectReferences(file);

    #endregion

}


//public class PackHelper(string[] directorySorces)
//{
//    private const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
//    private readonly string[] directorys = (directorySorces ?? []).OfType<string>().ToArray();

//    private readonly static string[] targetFiles = [".sln", ".csproj"];

//    /// <summary>
//    /// 是否为支持的文件扩展名
//    /// </summary>
//    public static bool IsTargetFile(string file) =>
//        targetFiles.Any(x => file.EndsWith(x, ignoreCase));

//    private IEnumerable<string> GetDirectorys(string name)
//    {
//        var items = directorys.Select(x => Path.Combine(x, name));
//        foreach (var item in items)
//            if (Directory.Exists(item))
//                yield return item;
//    }

//    public IEnumerable<string> GetPackDirectorys(string path)
//    {
//        var csprojFiles = GetCsprojFiles(path).Distinct();
//        return csprojFiles
//            .SelectMany(x => GetInclude(x, "PackageReference"))
//            .SelectMany(GetDirectorys).Distinct().Order();
//    }

//    private static IEnumerable<string> GetCsprojFiles(string path)
//    {
//        if (path.EndsWith(".sln", ignoreCase))
//            return GetSlnItems(path);

//        if (path.EndsWith(".csproj", ignoreCase))
//            return GetCsprojIncludeFiles(path);

//        return [];
//    }

//    private static IEnumerable<string> GetCsprojIncludeFiles(string csproj)
//    {
//        var basePath = GetBasePath(csproj);
//        var files = GetInclude(csproj, "ProjectReference")
//             .Select(x => Path.GetFullPath(x, basePath));

//        return files.SelectMany(GetCsprojIncludeFiles).Prepend(csproj);
//    }

//    private static IEnumerable<string> GetInclude(string csproj, string target)
//    {
//        var doc = new XmlDocument();
//        doc.Load(csproj);
//        var refs = doc.SelectNodes($"//{target}")?.Cast<XmlNode>() ?? [];
//        foreach (var item in refs)
//        {
//            if (TryGetInclude(item, out var include))
//                yield return include;
//        }
//    }

//    private static bool TryGetInclude(XmlNode xml, out string include)
//    {
//        include = string.Empty;
//        var items = xml.Attributes?.Cast<XmlAttribute>() ?? [];
//        foreach (XmlAttribute item in items)
//        {
//            var name = item.Name;
//            if (item.Name.Equals("Include", ignoreCase))
//            {
//                include = item.Value;
//                return true;
//            }
//        }
//        return false;
//    }

//    private static IEnumerable<string> GetSlnItems(string slnPath)
//    {
//        var basePath = GetBasePath(slnPath);
//        var content = File.ReadAllText(slnPath);
//        foreach (var item in Search(content, "Project", "EndProject"))
//        {
//            var path = item.Split(',').ElementAtOrDefault(1) ?? "";
//            path = path.Trim(['\"', ' ']).Trim();
//            if (!path.EndsWith(".csproj", ignoreCase)) continue;

//            var fullPath = Path.GetFullPath(path, basePath);
//            if (File.Exists(fullPath)) yield return fullPath;
//        }
//    }

//    private static string GetBasePath(string path)
//    {
//        return Path.GetDirectoryName(path)
//            ?? throw new NotImplementedException($"路径不正确:{path}");
//    }

//    private static IReadOnlyCollection<string> Search(string content, string start, string end,
//      StringComparison comparison = StringComparison.OrdinalIgnoreCase)
//    {
//        var span = content.AsSpan();
//        var res = new List<string>();

//        while (true)
//        {
//            var s = span.IndexOf(start, comparison);
//            if (s is -1) break;
//            s += start.Length;

//            var e = span.IndexOf(end, comparison);
//            if (e is -1) break;

//            res.Add(span[s..e].ToString());
//            span = span[(e + end.Length)..];
//        }
//        return res;
//    }
//}