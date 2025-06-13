using System.Diagnostics.CodeAnalysis;

namespace ZhApi.Cores;
public  class IgnoreCaseEqualityComparer : IEqualityComparer<string>
{
    public static readonly IgnoreCaseEqualityComparer Instance = new();

    public bool Equals(string? x, string? y) =>
        string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode([DisallowNull] string obj) =>
        obj.ToLower().GetHashCode();  
}