namespace ZhApi.Configs;
public record class ModelInfo(string Info, string ModelName)
{
    public bool EqualsModelName(ModelInfo info) =>
        info.ModelName.Equals(ModelName, StringComparison.OrdinalIgnoreCase);

    public readonly static ModelInfo ImportDataBase = new("DataBase", "DataBase");
}