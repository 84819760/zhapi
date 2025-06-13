namespace ZhApi.Messages;

public record class MessageBase
{
}

public record class MessageTargetBase(object Target)
    : MessageTargetBase<object>(Target);

public record class MessageTargetBase<T>(T Target): MessageBase;


public record class Message<T>(string Content) : MessageBase
{

}

public record class ContentMessage(string Message): MessageBase;