namespace ZhApi.WpfApp.LogItems;
public partial class LogControlViewModel : ControlProvider
{
    [ObservableProperty]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial Brush Foreground { get; set; } = (Brush)App.Current.Resources["Textdim"];

    [ObservableProperty]
    public partial Brush? Background { get; set; }

    [ObservableProperty]
    public partial string? ToolTip { get; set; }

    public LogControlViewModel SetToolTip(string? toolTip)
    {
        ToolTip = toolTip;
        return this;
    }

    public LogControlViewModel SetStart(string? text = null) => Set(text, "TextE6");

    public LogControlViewModel SetEnd(string? text = null) => Set(text, "Textdim");

    public LogControlViewModel Set(string? text, string foreground)
    {
        if (text != null) Text = text;
        Foreground = (Brush)App.Current.Resources[foreground];
        return this;
    }

    protected override Control CreateControl() =>
        new LogControl() { DataContext = this };
}