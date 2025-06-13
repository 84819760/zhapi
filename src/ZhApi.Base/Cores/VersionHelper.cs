using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Reflection;
using ZhApi.Configs;

namespace ZhApi.Cores;
[AddService(ServiceLifetime.Singleton)]
public partial class VersionHelper : ObservableObject
{
#if true
    private const string urlDafult = "https://gitee.com/84819760/zh-api/releases/latest";
#else
    private const string urlDafult = "https://gitee.com/84819760/test/releases/latest";
#endif

    private readonly string url;
    public VersionHelper(IOptionsSnapshot<AppConfig> options)
    {
        url = options.Value.VersionUrl ?? urlDafult;
        Version = $"V{GetAppVersion()}";
        _ = TestUpdate();
    }

    public string Version { get; }

    [ObservableProperty]
    public partial string? UpdatePrompt { get; set; }

    [ObservableProperty]
    public partial string? UpdateUrl { get; set; }

    [RelayCommand]
    public void ToUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = UpdateUrl,
                UseShellExecute = true
            });
        }
        catch (Exception)
        { }
    }

    private static string GetAppVersion() =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "未知";

    private async Task TestUpdate()
    {
        var uri = await GetUriAsync();
        if (uri is null) return;
        var finalUrl = uri.Segments.LastOrDefault();
        if (Version.EqualsIgnoreCase(finalUrl)) return;
        UpdatePrompt = $"检测到新版本: {finalUrl}";
        UpdateUrl = uri.AbsoluteUri;
    }

    private async Task<Uri?> GetUriAsync()
    {
        const HttpCompletionOption option = HttpCompletionOption.ResponseHeadersRead;
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url, option);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            return response.RequestMessage?.RequestUri;
        }
        catch (Exception)
        {
            return null;
        }
    }

}
