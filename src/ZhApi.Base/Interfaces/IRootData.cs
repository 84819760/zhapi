using LiteDB;

namespace ZhApi.Interfaces;
public interface IRootData
{
    string Index { get; }

    string OriginalXml { get; }

    PathInfo PathInfo { get; }

    List<ScoreData> Items { get; }

    object? Tag { get; set; }

    ScoreData? GetScore() => Items.OrderBy(x => x.Value).FirstOrDefault();

    /// <summary>
    /// 获取评分最高的key
    /// </summary>
    KeyData GetKey() => GetScore()?.Key ?? new KeyData()
    {
        Index = Index,
        Value = 9527
    };
}

public class RootData : IRootData
{
    public object? Tag { get; set; }

    public required string Index { get; init; }

    public required string OriginalXml { get; init; }

    public required PathInfo PathInfo { get; init; }

    public List<ScoreData> Items { get; } = [];

}