using System.Diagnostics;
using ZhApi.Configs;

namespace ZhApi.Cores;
[DebuggerDisplay("{GetRetryTest()} | id:{Detail.ResponseData.RequestId}")]
public class ScoreData
{
    private string? retryTest;

    public required ScoreDataDetail Detail { get; init; }

    public required string ErrorSimple { get; init; }

    public required double Value { get; init; }

    public required string Xml { get; init; }

    public required KeyData Key { get; init; }

    public string GetRetryTest() =>
        retryTest ??= $"{Value} | {ErrorSimple} | {GetInfo()}";

    public string GetInfo() => Detail.ResponseData.ModelInfo.Info;

    public bool IsFail => Key.IsFail;

    public bool IsPerfect => Key.IsPerfect;

    public string GetRetryMessage() => Detail.GetRetryMessage();


    public bool IsTarget(Target target) => Key.IsTarget(target);

    public string GetLog()
    {
        var log = Detail.GetErrorList();
        return $"[v:{Value}, len:{Xml?.Length ?? 0}, {{{log}}}, id:{Detail.ResponseData.RequestId}]";
    }
}
