using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using ZhApi.Messages;

namespace ZhApi;
public partial class Extends
{
    /// <summary>
    /// 格式化 1.5s
    /// </summary>
    public static string Foramt(this TimeSpan time,
        string foramtSeconds = "{0:N0}",
        string foramt = "{0:N1}",
        string d = "d", string h = "h", string m = "m", string s = "s")
    {
        if (time.Days > 0)
            return string.Format($"{foramt}{d}", time.TotalDays);

        if (time.Hours > 0)
            return string.Format($"{foramt}{h}", time.TotalHours);

        if (time.Minutes > 0)
            return string.Format($"{foramt}{m}", time.TotalMinutes);

        return string.Format($"{foramtSeconds}{s}", time.TotalSeconds);
    }

    public static string ForamtCropping(this TimeSpan time)
    {
        if (time.TotalSeconds <= 0) return "0s";
        if (time.Days > 0)
            return $"{time:d\\dhh\\hmm\\mss\\s}";

        if (time.Hours > 0)
            return $"{time:h\\hmm\\mss\\s}";

        if (time.Minutes > 0)
            return $"{time:m\\mss\\s}";

        return $"{time:s\\s}";
    }


    public static void SendMessage<T>(this T v) where T : MessageBase
    {
        try
        {
            WeakReferenceMessenger.Default.Send(v);
        }
        catch (Exception)
        {
            Debug.WriteLine("未处理异常");
            throw;
        }
    }
}