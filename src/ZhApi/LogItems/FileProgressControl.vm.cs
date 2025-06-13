using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZhApi.Cores;

namespace ZhApi.WpfApp.LogItems;
public partial class FileProgressControlViewModel
    : ControlProvider, IRecipient<FileProgressMessage>
{
    private readonly HashSet<string> paths = [];
    private DateTime time = DateTime.Now;
    private bool isEnd;

    public FileProgressControlViewModel()
    {
        _ = ShowSummary();
    }

    #region Props

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? FileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Length { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial double Current { get; set; }

    [ObservableProperty]
    public partial Visibility Visibility { get; set; } = Visibility.Collapsed;

    /// <summary>
    /// 进度显示
    /// </summary>
    [ObservableProperty]
    public partial Visibility ProgresstVisibility { get; set; } = Visibility.Visible;


    [ObservableProperty]
    public partial string? Summary { get; set; }

    public double Progress => (Current / Length).UnNaN();
    #endregion

    protected override Control CreateControl() =>
        new FileProgressControl() { DataContext = this };

    void IRecipient<FileProgressMessage>.Receive(FileProgressMessage message)
    {
        if (message.Title != Title) return;
        time = DateTime.Now.AddSeconds(10);
        paths.Add($"{Title}:{message.FilePath}");
        Visibility = Visibility.Visible;
        FilePath = message.FilePath;
        Length = message.Total;
        Current = message.Current;
        ProgresstVisibility = Length > 1000 ? Visibility.Visible : Visibility.Collapsed;
        //FilePath = message.FilePath;
    }


    // 英文字母开头
    [GeneratedRegex(@"[0-9]")]
    public static partial Regex RegexNumeral();

    partial void OnFilePathChanged(string? value)
    {
        if (value is null) return;
        var fileName = Path.GetFileName(value);
        var dir = Path.GetDirectoryName(value) ?? "";
        var v = dir.Split('\\', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => RegexNumeral().IsMatch(x))
            .Reverse().Take(2).JoinString();

        if (v is { Length: > 0 }) v = $"({v})";

        FileName = $"{fileName} {v}";
    }

    //  延迟关闭
    private async Task ShowSummary()
    {
        while (!isEnd)
        {
            await Task.Delay(2000);
            if (DateTime.Now < time) continue;
            Visibility = Visibility.Collapsed;
        }
    }

    public override Task EndHandlerAsync(Exception? ex = null)
    {
        isEnd = true;
        Visibility = Visibility.Collapsed;
        return base.EndHandlerAsync(ex);
    }
}