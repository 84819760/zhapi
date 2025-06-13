using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ZhApi;
public partial class Extends
{
    private static void InitNewtonsoftJson() =>
    JsonConvert.DefaultSettings = () => new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        Formatting = Newtonsoft.Json.Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatString = "yyyy-MM-dd HH:mm:ss",
        Converters = { new StringEnumConverter() }
    };

    public static string SerializeNewtonsoftJson<T>(T value)
    {
        return JsonConvert.SerializeObject(value);
    }

}