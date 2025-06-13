namespace ZhApi.MicrosoftAI.Base;
internal class ChatMessageHelp(ChatMessage[] messages)
{
    private readonly ChatMessage[] messages = messages
        .Where(x => x.Role.Value != "system").ToArray();

    public override string ToString() =>
        messages.Select(x => $"{x.Role.Value} : {x.Text}").JoinString("\r\n");
}
