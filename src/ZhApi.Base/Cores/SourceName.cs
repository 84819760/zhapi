using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ZhApi.Cores;

[Table("Sources")]
[Index(nameof(Name), IsUnique = true)]
public class SourceName
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public required string Name { get; init; }
}