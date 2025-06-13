using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ZhApi.Bases;

namespace ZhApi;
partial class Extends
{

    #region Sha256
    public static string GetFileSha256String(this FileInfo file)
    {
        using var fs = file.OpenRead();
        var hs = SHA256.HashData(fs);
        return Convert.ToBase64String(hs);
    }

    /// <summary>
    /// 返回字符串Sha256
    /// </summary>
    /// <remarks><see cref="long"/></remarks>
    public static long GetSha256Int64(this string value)
    {
        var bytes = value.GetSha256();
        return BitConverter.ToInt64(bytes);
    }

    /// <summary>
    /// 返回字符串Sha256
    /// </summary>
    /// <remarks><see cref="byte[]"/></remarks>
    public static byte[] GetSha256(this string value)
    {
        var txt = value.ToLower().Trim();
        var utf8 = Encoding.UTF8.GetBytes(txt);
        return SHA256.HashData(utf8);
    }

    /// <summary>
    /// 返回字符串Sha256
    /// </summary>
    /// <remarks>2TDTyFoLGi+g9Msmdr/0MgDW7NlnhOhWW0CdSd8jUUM= 44</remarks>
    public static string GetSha256Base64(this string value)
    {
        var bytes = value.GetSha256();
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 返回字符串Sha256
    /// </summary>
    /// <returns>EB73E8CDBFCC26E5848EA3DA0A63345DF639B6C810E101B574D0C974B1916123 64</returns>
    public static string GetSha256Hex(this string value)
    {
        var bytes = value.GetSha256();
        return Convert.ToHexString(bytes);
    }

    #endregion

    #region 包含中文
    [GeneratedRegex(@"[\u4E00-\u9FFF]")]
    private static partial Regex ZhCnRegex();

    /// <summary>
    /// 是否包含中文
    /// </summary>
    public static bool IsContainChinese(this string txt)
    {
        if (txt is not { Length: > 0 }) return false;
        return ZhCnRegex().IsMatch(txt);
    }
    #endregion

    public static string GetSimpleId(this Guid guid)
    {
        var array = guid.ToByteArray()[..6];
        return Convert.ToHexString(array);
    }


    /// <summary>
    /// string.Join
    /// </summary>
    public static string JoinString<T>(this IEnumerable<T> items,
        string separator = ", ") => string.Join(separator, items);

    /// <summary>
    /// 返回字符串中的行(末尾可能有空行)
    /// </summary>
    public static IEnumerable<string> GetLines(this string v)
    {
        using var sr = new StringReader(v);
        string? res;
        while ((res = sr.ReadLine()) is not null)
            yield return res;
    }


    private static readonly string[] bs =
        ["B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB"];

    public static string FormatSize(this double v) => FormatSize((long)v);

    public static string FormatSize(this long length)
    {
        var position = 0;
        double number = length;
        while (Math.Round(number / 1024, 4) >= 1)
        {
            number /= 1024;
            position++;
        }
        return $"{string.Format("{0:0.0}", number)}{bs[position]}".Replace(".0", "");
    }

    public static async Task ShowSimpleId<T>(this T obj, string title, Func<Task> task)
    {
#if DEBUG
        var hc = obj?.GetHashCode();
        var type = obj?.GetType()?.FullName;
        var gid = Guid.NewGuid().GetSimpleId();
        var id = $"{gid} {hc:D10} {type}";
        Debug.WriteLine($"{id} -> {title}");
#endif
        await task();
#if DEBUG
        Debug.WriteLine($"{id} <- {title}");
#endif
    }

    #region ObjectPool<StringBuilder>
    public readonly static ObjectPool<StringBuilder> StringBuilderPool =
    new DefaultObjectPoolProvider().CreateStringBuilderPool();

    public static string StringBuild(Action<StringBuilder> action)
    {
        var sb = StringBuilderPool.Get();
        try
        {
            action(sb);
            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    public static string ListPool(Func<List<string>, string> func)
    {
        var item = ObjectPoolHelper<List<string>>.Get();
        try
        {
            return func(item);
        }
        finally
        {
            item.Clear();
            ObjectPoolHelper<List<string>>.Return(item);
        }
    }
    #endregion 

    public static bool EqualsIgnoreCase(this string value, string? target)
    {
        return value.Equals(target, StringComparison.OrdinalIgnoreCase);
    }


    public static string GetXmlMarkdown(this string value) => $"""
    ```xml
    <root>
    {value}
    </root>
    ```
    """;
}