using System.Threading.Channels;

namespace ZhApi.Services;

/// <summary>
/// 负责创建预读内容
/// </summary>
[AddService(ServiceLifetime.Scoped)]
public class FileService : IDisposable
{
    private readonly IDbContextFactory<KvDbContext> dbFactory;
    private readonly ChannelTimeHandler<IRootData> channel;
    private readonly XmlFileBuildService xmlFile;
    private readonly IDataService dataService;
    private readonly CancellationToken token;
    private readonly IdProvider idProvider;
    private readonly FileQueue fileQueue;
    private readonly Mickey mickey;
    private readonly int maxLength;

    public FileService(IdProvider idProvider,
        IDbContextFactory<KvDbContext> dbFactory,
        IOptionsSnapshot<AppConfig> appConfig,
        IDataService dataService, Mickey mickey,
        FileQueue fileQueue, XmlFileBuildService xmlFile,
        CancellationTokenSource cts)
    {
        this.idProvider = idProvider;
        this.dbFactory = dbFactory;
        this.dataService = dataService;
        this.fileQueue = fileQueue;
        this.xmlFile = xmlFile;
        this.mickey = mickey;

        maxLength = appConfig.Value.MaxLength ?? 5000;
        token = cts.Token;
        channel = new ChannelTimeHandler<IRootData>(Handler, 15, token);
    }

    public async Task SendAsync(XmlFileInfo info)
    {
        GC.Collect();
        var filePack = new FileData(info, idProvider.GetIdCode());
        await CreateReadAhead(filePack);
        await xmlFile.SendAsync(filePack);

        if (token.IsCancellationRequested) return;
        new IncreaseFile().SendMessage();

        var count = filePack.Count;
        if (count is 0) return;
        SendMessage(filePack, count);
    }

    private static void SendMessage(FileData file, int count)
    {
        var xmlPath = file.XmlFile;
        var msg = $"添加文件 {xmlPath} ({count:N0})";
        FileLog.Default.LogDebug(msg);
        new LogMessage(msg).SendMessage();
    }

    private async Task CreateReadAhead(FileData readAheadFile)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var tab = db.KvRows.AsNoTracking();
        var items = GetReadAheadServices(readAheadFile, tab);

        await foreach (var item in items)
            await channel.SendAsync(item);
    }

    // 9.0 Mono.Android.xml  132,750
    private async IAsyncEnumerable<IRootData> GetReadAheadServices(
       FileData readFile, IQueryable<KvRow> tab)
    {
        var count = 0;
        var file = readFile.XmlFileInfo;
        var total = file.AssemblyInfo.MemberCount;
        var info = file.AssemblyInfo;
        double progress = 0;

        foreach (var member in info.GetMemberNodes())
        {
            count++;

            var p = count.GetProgress(total);
            if (p != progress)
            {
                progress = p;
                SendMessage(file, count, total);
            }

            foreach (var item in member.GetSnippetUnitNodes())
            {
                if (token.IsCancellationRequested) yield break;

                // 超过长度
                if (IsExceed(item)) continue;

                var sha256Code = item.Sha256Code;
                var isExist = mickey.Exist(sha256Code, readFile, out var rf);
                readFile.ReadAheadFiles.Add(rf);

                if (isExist || IsSkip(item) || await ExistsAsync(item, tab))
                    continue;

                readFile.Count += 1;
                yield return new RootData()
                {
                    PathInfo = item.Node.Info,
                    OriginalXml = item.Key,
                    Index = sha256Code,
                };
            }
        }
        SendMessage(file, count, total);
    }

    private bool IsExceed(UnitNode node)
    {
        var length = node.Key.Length;
        if (maxLength > length) return false;

        var info = node.Node.Info;
        FileLog.Default.LogDebug("""
            超出长度 {maxLength} {length}
            file: '{file}'
            path: '{path}'
            """,
            maxLength, length,
            info.FilePath, info.MemberPath
        );
        return true;
    }

    private static void SendMessage(XmlFileInfo info, int count, int total) =>
        new FileProgressMessage("读取文件", info, count, total).SendMessage();

    private static bool IsSkip(UnitNode node) =>
        node.WordKeys.Count is 0 || node.Key.IsContainChinese();

    private static Task<bool> ExistsAsync(UnitNode node, IQueryable<KvRow> tab) =>
        tab.IsExistIdAsync(node.Sha256Code);

    public async Task Completion()
    {
        await channel.Completion();
        await fileQueue.Completion();
        await xmlFile.Completion();
    }

    public void Dispose()
    {
        channel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected async Task Handler(IRootData[] values)
    {
        await dataService.InsertBulkAsync(values);
        foreach (var item in values)
        {
            var key = item.GetKey();
            await fileQueue.SendAsync(key);
        }
    }
}