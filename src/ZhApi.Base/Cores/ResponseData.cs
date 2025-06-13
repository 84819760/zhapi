using Microsoft.Extensions.AI;
using System.Diagnostics;
using ZhApi.Configs;

namespace ZhApi.Cores;

[DebuggerDisplay("{RequestIndex} {Response}")]
public record class ResponseData
{
    public required ChatMessage[] Messages
    {
        get => field;
        set => field = GetMessages(value);
    }

    public required ModelInfo ModelInfo { get; init; }

    public required long RequestId { get; init; }

    public string Response { get; set; } = string.Empty;

    public TimeSpan Time { get; set; }

    public string? Exception { get; set; }

    public bool IsTimeout { get; set;}

    private static ChatMessage[] GetMessages(IEnumerable<ChatMessage> chatMessages) =>
        chatMessages.Where(x => !x.Role.Equals(ChatRole.System)).ToArray();

    public override string ToString() => Extends.StringBuild(sb =>
    {
        sb.AppendLine($"id:{RequestId}, {ModelInfo.Info}");
        sb.AppendLine("请求:");
        foreach (var item in Messages)
            sb.AppendLine($"{item.Role} : {item.Text}");

        sb.AppendLine("响应:").AppendLine(Response);
        if (Exception is null) return;
        sb.Append("请求时异常: ").AppendLine(Exception);
    });

    public static ResponseData Create(ChatMessage[] messages, ModelInfo modelInfo, int requestId)
    {
        return new() { ModelInfo = modelInfo, Messages = messages, RequestId = requestId };
    }
}
