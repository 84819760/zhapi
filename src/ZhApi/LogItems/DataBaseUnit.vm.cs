using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace ZhApi.WpfApp.LogItems;
public partial class DataBaseUnitViewModel :
    ObservableRecipientActive, IRecipient<DataBaseMessage>
{
    private readonly static Lock @lock = new();
    private ColorAnimation? changedAnimation;
    private ColorAnimation? valueAnimation;
    private SolidColorBrush? brushChanged;
    private SolidColorBrush? brushValue;

    private readonly ExponentialEase easingFunction = new()
    {
        Exponent = 9,
        EasingMode = EasingMode.EaseInOut,
    };

    /// <summary>
    /// 图标
    /// </summary>
    [ObservableProperty]
    public partial SymbolRegular Symbol { get; set; } = SymbolRegular.DatabaseArrowRight20;

    [ObservableProperty]
    public partial Duration Duration { get; set; } = TimeSpan.FromSeconds(5);

    [ObservableProperty]
    public partial double IconFontSize { get; set; } = 19;

    [ObservableProperty]
    public partial double FontSize { get; set; } = 14;

    public string Target { get; internal set; } = string.Empty;

    #region Color   
    public Color ValueFromColor { get; set; } = "#FFa0a0a0".ToColor();

    public Color ValueToColor { get; set; } = Colors.Gray;

    // LightGreen  LightPink
    public Color ChangedFromColor { get; set; } = Colors.LightPink;
    public Color ChangedToColor { get; set; } = Colors.DimGray;
    #endregion

    #region Brush  
    public SolidColorBrush ValueBrush
    {
        get => brushValue ??= new(ValueToColor);
        set
        {
            brushValue = value;
            OnPropertyChanged();
        }
    }

    public SolidColorBrush ChangedBrush
    {
        get => brushChanged ??= new(ChangedToColor);
        set
        {
            brushChanged = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Animation   
    public ColorAnimation ValueAnimation
    {
        get => valueAnimation ??= new()
        {
            From = ValueFromColor,
            To = ValueToColor,
            Duration = Duration,
            EasingFunction = easingFunction,
        };
        set
        {
            valueAnimation = value;
            OnPropertyChanged();
        }
    }

    public ColorAnimation ChangedAnimation
    {
        get => changedAnimation ??= new()
        {
            From = ChangedFromColor,
            To = ChangedToColor,
            Duration = Duration,
            EasingFunction = easingFunction,
        };
        set
        {
            changedAnimation = value;
            OnPropertyChanged();
        }
    }
    #endregion

    [ObservableProperty]
    public partial int Count { get; set; }

    [ObservableProperty]
    public partial int ChangedValue { get; set; }

    [ObservableProperty]
    public partial Visibility Visibility { get; set; } = Visibility.Collapsed;

    partial void OnCountChanged(int oldValue, int newValue)
    {
        if (oldValue == newValue) return;
        ChangedValue = newValue - oldValue;
        Visibility = Visibility.Visible;
        this.AppBeginInvoke(() =>
        {
            ValueBrush.BeginAnimation(SolidColorBrush.ColorProperty, ValueAnimation);
            ChangedBrush.BeginAnimation(SolidColorBrush.ColorProperty, ChangedAnimation);
        });
    }

    void IRecipient<DataBaseMessage>.Receive(DataBaseMessage message)
    {
        if (message.Target != Target) return;
        lock (@lock) Count += message.Count;
    }
}