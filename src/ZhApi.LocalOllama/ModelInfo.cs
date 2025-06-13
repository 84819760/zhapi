using Newtonsoft.Json;
using Pathoschild.Http.Client;
using System.Text.Json.Serialization;

namespace ZhApi.LocalOllama;
public class ModelDetails
{
    [JsonPropertyName("details")]
    [JsonProperty("details")]
    public required DetailsRow Details { get; set; }

    public string RunName { get; set; } = string.Empty;

    public class DetailsRow
    {
        [JsonPropertyName("parent_model")]
        [JsonProperty("parent_model")]
        public required string ParentModel { get; set; }

        [JsonPropertyName("family")]
        [JsonProperty("family")]
        public required string Family { get; set; }


        [JsonPropertyName("parameter_size")]
        [JsonProperty("parameter_size")]
        public required string ParameterSize { get; set; }

        [JsonPropertyName("quantization_level")]
        [JsonProperty("quantization_level")]
        public required string QuantizationLevel { get; set; }

    }

    public string ModelName => Details.ParentModel is { Length: > 0 } ? Details.ParentModel : RunName;

    public static async Task<ModelDetails> GetMethodName(
        FluentClient client, string modelName)
    {
        var res = await client
            .PostAsync("/api/show", new { model = modelName })
            .As<ModelDetails>();
        res.RunName = modelName;
        return res;
    }
}