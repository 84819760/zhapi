using System.Globalization;
using System.Windows.Data;

namespace ZhApi.WpfApp;
public class NotVisibility : IValueConverter
{
    public object Convert(object value, Type _t, object _p, CultureInfo _c)
    {
        if (value is not Visibility v)
            throw new ArgumentException("参数不是 Visibility");
        return v == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object _, Type _t, object _p, CultureInfo _c) =>
        throw new NotImplementedException();
}

/// <summary>
/// 空值或默认值不显示
/// </summary>
public class NotDefautVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return Visibility.Collapsed;

        if (value is string v)
            return v is { Length: > 0 } ? Visibility.Visible : Visibility.Collapsed;

        if (double.TryParse($"{value}", out var double_value))
            return double_value != 0 ? Visibility.Visible : Visibility.Collapsed;

        throw new NotImplementedException($"未处理类型{value.GetType()}");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 秒格式化(只显示3位)
/// </summary>
public class SecondFormat : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double d) return 0;
        if (double.IsNaN(d)) return 0;
        // 10.0s
        if (d >= 10d) return $"{d:N1}s";
        // 1.10s
        return $"{d:N2}s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// 百分比
/// </summary>
public class PercentageFormat : IValueConverter
{
    public readonly static PercentageFormat Default = new();

    public object Convert(object value) =>
        Convert(value, null!, null!, null!);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double d) return "";
        if (double.IsNaN(d)) return $"0%";
        if (d > 0.999d && d < 1.0d) return $"99.9%";
        //  100%
        if (d >= 1.0d) return $"{d:P0}";
        // 10.0%
        if (d >= 0.1d) return $"{d:P1}";
        // 0.01%
        return $"{d:P2}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}