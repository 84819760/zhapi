namespace ZhApi.Configs;
public partial class ConfigBase
{
    public string GetName() => Title ?? Model?.Trim() ?? Type ?? DefaultName;

    public string GetModel() => Model?.Trim() ?? DefaultName;

    private string? GetModelValue()
    {
        var res = $"{GetModel().Trim(DefaultName)}";
        return res is { Length: > 0 } ? res : null;
    }

    public ModelInfo GetModelInfo() => new(Info, GetModel());

    public virtual string? GetError()
    {
        if (Model is not { Length: > 0 } || Model is DefaultName)
            return $"未配置(model : ?)";

        return GetUrlError();
    }

    protected virtual string? GetUrlError()
    {
        if (Url is not { Length: > 0 })
            return $"未配置(url : ?)";

        return default;
    }

    /// <summary>
    /// 用于关闭apikey等敏感信息
    /// </summary>
    public virtual object GetLog() => this;

    /// <summary>
    /// 返回提示词(用于扩展)
    /// </summary>
    public virtual string? GetSystemPrompt() => default;

    public void Test() => ExceptionMessage = GetError();
}
