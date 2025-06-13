using System;

namespace ZhApi.Cores;

/// <summary>
/// 要忽略翻译的单词
/// </summary>
public static partial class IgnoreWords
{
    private const string file = "zhapi\\ignore_words.txt";
    private const StringSplitOptions options =
        StringSplitOptions.RemoveEmptyEntries |
        StringSplitOptions.TrimEntries;

    /// <summary>
    /// 待补充(修改为本地数据库读取,或者文本)
    /// </summary>
    private readonly static HashSet<string> ignoreWords;

    static IgnoreWords()
    {
        TryCreateFile();
        ignoreWords = File.ReadAllText(file, Encoding.Default).Split(',')
            .Select(x => x.Trim()).Where(x => x is { Length: > 0 })
            .ToHashSet(IgnoreCaseEqualityComparer.Instance);
    }

    private const string ignores = """
    x,y,z,xy,xyz,
    Windows,NTFS,win32,Unix,Android,Google,
    Framework,Microsoft,
    office,null,true,false,java,js,javascript,http, https,
    ui,xml,resx,Web,Internet,Explorer,Bitmap,
    cookie,Visual,Vista, Studio,Unicode,px,Alt,
    Shift,Ctrl,None,Enter,Fn,catch,throw,Exception,Attribute,
    void,Keys,Key,lambda,with,min, max, value,
    Excel,Word,uid,
    uri,url,Uris,apps,id,html, 
    """;

    private static void TryCreateFile()
    {
        if (File.Exists(file)) return;

        var ws = ignores
            .Split([',', '\r', '\n'], options)
            .Select(x => x.Trim())
            .Where(x => x is { Length: > 0 })
            .ToArray();

        Write(ws);
    }

    private static void Write(string[] ws)
    {
        using var fs = File.OpenWrite(file);
        using var sw = new StreamWriter(fs, Encoding.Default);
        foreach (var item in ws.Chunk(6))
            sw.WriteLine($"{item.JoinString()},");
    }

    public static bool Contains(string v) => ignoreWords.Contains(v);

}