using ZhApi.Interfaces;

namespace ZhApi;
public static class RootDataHelper
{
    public static string GetLog(this IRootData rootData) =>
    Extends.StringBuild(sb =>
    {
        var gs = rootData.Items.GroupBy(x => x.Detail.ResponseData.ModelInfo.Info);
        foreach (var g in gs)
        {
            sb.AppendLine($"{g.Key} :");
            foreach (var item in g)
                sb.Append(' ').AppendLine(item.GetLog());
        }
    });

    public static bool IsTimeout(this IRootData rootData) => 
        rootData.Items.Any(x => x.Detail.ResponseData.IsTimeout);
}
