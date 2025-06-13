namespace ZhApi.Interfaces;
public interface IModelPorvider
{
    /// <summary>
    /// 返回模型列表
    /// </summary>
    Task<string[]> GetModelsAsync();
}
