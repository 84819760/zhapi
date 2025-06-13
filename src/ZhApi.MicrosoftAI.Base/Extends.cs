namespace ZhApi.MicrosoftAI.Base;
public static class Extends
{
    public static async Task InitAsync(this ITranslateService[] services)
    {
        foreach (var item in services)
            await item.InitAsync();

        // 启动第一个单例服务
        services.OfType<TranslateServiceChannel>()
            .FirstOrDefault()?.Start();
    }
}
