namespace ZhApi.Cores;
/// <summary>
/// 负责提供 <see cref="ITranslateService"/> Keyd 服务注册列表
/// </summary>
public record TranslateServiceNames(IReadOnlySet<string> Names);