using System.ComponentModel.DataAnnotations.Schema;

namespace ZhApi.Bases;

public class KvRowBase : TranslationData
{

    /// <summary>
    /// 评分
    /// </summary>
    [Column(Order = 2)]
    public required double Score { get; set; }


    /// <summary>
    /// 修复次数
    /// </summary>
    [Column(Order = 4)]
    public int RepairCount { get; set; }  
}

