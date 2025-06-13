namespace ZhApi.Interfaces;
public interface IDataService : IDisposable
{
    Task InsertBulkAsync(IEnumerable<IRootData> datas);

    Task InsertAsync(IRootData data);

    Task UpdateAsync(IRootData data);

    IRootData GetRootData(KeyData keyData);

}
