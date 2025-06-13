using ZhApi.Cores;
namespace ZhApi.DataService;

internal class RootData : IRootData
{
    public required ObjectId Id { get; init; }

    public List<ScoreData> Items { get; init; } = [];

    public required string OriginalXml { get; init; }

    public required PathInfo PathInfo { get; init; }

    public required string Index { get; init; }

    public object? Tag { get; set; }

}
