namespace ZhApi.API.OpenAI;

public class ModelInfo
{
    public required Datum[] Data { get; init; }
}

public class Datum
{
    public required string Id { get; init; }
}
