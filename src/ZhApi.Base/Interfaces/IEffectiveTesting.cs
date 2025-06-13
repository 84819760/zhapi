using ZhApi.Configs;
namespace ZhApi.Interfaces;

/// <summary>
/// 有效性测试
/// </summary>
public interface IEffectiveTesting
{
    Task<bool> TestAsync();
}