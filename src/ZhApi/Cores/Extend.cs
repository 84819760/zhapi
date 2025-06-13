namespace ZhApi;

public static class Extend
{

    public static void AppInvoke(this object _, Action action)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        catch (Exception) { }
    }


    public static void AppBeginInvoke(this object _, Action action)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        catch (Exception) { }
    }

    public static SolidColorBrush ToSolidColorBrush(this string colorCode)
    {
        return new(colorCode.ToColor());
    }

    public static Color ToColor(this string code) => (Color)ColorConverter.
        ConvertFromString(code);


    public static T? Call<T>(this T? value, Action<T> action)
    {
        if (value != null) action(value);
        return value;
    }
}