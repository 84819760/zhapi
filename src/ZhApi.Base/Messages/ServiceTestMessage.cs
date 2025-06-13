namespace ZhApi.Messages;

public record class ServiceTestMessage(object Target, string? Title) 
    : MessageTargetBase(Target);