using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace ZhApi;

public static class JsonHelper
{
    private static JsonSerializerOptions CreateSerializeOptions()
    {
        var res = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        res.Converters.Add(new JsonStringEnumConverter());
        res.Converters.Add(new JsonDateTimeConverter());
        return res;
    }

    private static JsonSerializerOptions CreateSerializeLogOptions()
    {
        var res = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true
        };
        res.Converters.Add(new JsonStringEnumConverter());
        res.Converters.Add(new JsonDateTimeConverter());
        return res;
    }

    private static readonly JsonSerializerOptions logSerializeOptions =
      CreateSerializeLogOptions();

    private static readonly JsonSerializerOptions serializeOptions =
        CreateSerializeOptions();

    private static readonly JsonSerializerOptions serializeOptionsIndented =
        new(CreateSerializeOptions()) { WriteIndented = true };

    /// <summary>
    /// 序列化
    /// </summary>
    public static string Serialize<T>(this T value, bool indented = false)
    {
        var options = indented ? serializeOptionsIndented : serializeOptions;
        return JsonSerializer.Serialize(value, options);
    }

    public static string SerializeLog<T>(this T value) =>
        JsonSerializer.Serialize(value, logSerializeOptions);

    /// <summary>
    /// 反序列化
    /// </summary>
    public static T? Deserialize<T>(this string json) =>
      JsonSerializer.Deserialize<T>(json, serializeOptions);

    public static Task SerializeEnumerableAsync<T>(this Stream stream,
      IAsyncEnumerable<T> items, CancellationToken token = default)
    {
        var options = serializeOptionsIndented;
        return JsonSerializer.SerializeAsync(stream, items, options, token);
    }

    public static Task SerializeEnumerableAsync<T>(this Stream stream,
        IEnumerable<T> items, CancellationToken token = default)
    {
        var options = serializeOptionsIndented;
        return JsonSerializer.SerializeAsync(stream, items, options, token);
    }

    public static IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(this Stream stream,
        CancellationToken token = default)
    {
        var options = serializeOptions;
        return JsonSerializer.DeserializeAsyncEnumerable<T>(stream, options, token);
    }

    /// <summary>
    /// 日期格式化
    /// </summary>
    internal class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _dateFormat = "yyyy-MM-dd HH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.TryParse(reader.GetString(), out var date) ? date : default;
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_dateFormat));
        }
    }

}