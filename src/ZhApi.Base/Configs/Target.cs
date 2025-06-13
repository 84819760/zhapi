namespace ZhApi.Configs;
/// <summary>
/// 代表服务要处理的类型
/// </summary>
[Flags]
public enum Target
{
    /// <summary>
    /// 全部
    /// </summary>
    All = 1 << 0,
    /// <summary>
    /// 未翻译的单词
    /// </summary>
    Word = 1 << 1,
    /// <summary>
    /// 节点丢失或多出
    /// </summary>
    Node = 1 << 2,
    /// <summary>
    /// 翻译失败
    /// </summary>
    Fail = 1 << 3,
}