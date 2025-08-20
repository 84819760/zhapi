using Newtonsoft.Json;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ZhApi.API.OpenAI;
public class Config : Configs.ConfigBase
{
    [JsonPropertyOrder(4)]
    [JsonProperty(Order = 4)]
    public string ApiKey { get; set; } = DefaultName;


    [JsonPropertyOrder(6)]
    [JsonProperty(Order = 6)]
    [DefaultValue(0)]
    public override int? Timeout { get; set; } = 180;

    [JsonPropertyOrder(6)]
    [JsonProperty(Order = 6)]
    public override int? MaxLength { get; set; } = 0;

    public override object GetLog()
    {
        var res = this.Serialize().Deserialize<Config>()!;
        res.ApiKey = ForamtApiKey(ApiKey);
        return res;
    }

    private static string ForamtApiKey(string apiKey) => Extends.StringBuild(sb =>
    {
        sb.Append(apiKey);
        var end = sb.Length - 5;
        for (var i = 5; i < end; i++) sb[i] = '*';
    });

    public override string? GetError() =>
        base.GetError() ?? GetUrlError() ?? GetApiKeyError();

    private string? GetApiKeyError()
    {
        if (ApiKey is not { Length: > 0 } || ApiKey is DefaultName)
            return $"未配置(apiKey : ?)";

        return default;
    }
}