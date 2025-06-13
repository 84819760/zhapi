using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZhApi.SqliteDataBase;
public class DataBaseVersion
{
    [Column(Order = 0)]
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public string Version { get; set; } = "1";
}
