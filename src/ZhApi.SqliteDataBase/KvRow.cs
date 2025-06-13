using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace ZhApi.SqliteDataBase;
[DebuggerDisplay("Score : {Score}, {Id}")]
public class KvRow : KvRowBase
{
    private string? id;
    [Key, Column(Order = 0, TypeName = "char(44)")]
    public string Id { get => GetId(); set => id = value; }

    [Column(Order = 1)]
    public required long SourceId { get; set; }

    [Column(Order = 3)]
    public string Tag { get; set; } = string.Empty;

    [Column(Order = 999, TypeName = "timestamp")]
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 保存完成后的回调函数
    /// </summary>
    [NotMapped]
    public Action? Callback { get; set; }

    private string GetId()
    {
        if (id is { Length: > 0 }) return id;
        var res = Original.GetSha256Base64();
        if (Original is { Length: > 0 }) id = res;
        return res;
    }
}