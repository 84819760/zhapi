using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using ZhApi.Configs;
using ZhApi.Interfaces;

namespace ZhApi.Bases;

[DebuggerDisplay("Translation : {Translation?.Trim().Length}, Original: {Original?.Trim().Length}")]
public class TranslationData
{
    [Column(Order = 5)]
    public required string Original { get; set; }

    [Column(Order = 6)]
    public required string Translation { get; set; }



    [NotMapped]
    public string? Describe { get; set; }

    public bool IsOk => Original?.Trim() is { Length: > 0 };
    // && Translation?.Trim() is { Length: > 0 };
    // && Translation.IsContainChinese();


    public ScoreData? CreateScore()
    {
        var root = new RootData
        {
            Index = Original.GetSha256Base64(),
            OriginalXml = Original.GetXmlMarkdown(),
            PathInfo = PathInfo.Default
        };

        root.CreateScore(ModelInfo.ImportDataBase, Translation);
        return ((IRootData)root).GetScore();
    }
}