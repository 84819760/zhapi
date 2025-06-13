using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZhApi.Cores;

namespace ZhApi;

partial class ShadowCodeInjectionExtensions
{
    static partial void UseStart(IServiceCollection service, IConfigurationManager? config)
    {
        service.AddScoped<CancellationTokenSource>();
        service.AddSingleton(_ => FileLog.Default);
    }
}