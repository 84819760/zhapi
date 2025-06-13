namespace ZhApi.Services;
[AddService(ServiceLifetime.Scoped)]
public class ScanService(XmlFileBuildService xmlFileBuildService,
    CancellationTokenSource cts, FileService fileService) : ICompletionTask
{
    private readonly CancellationToken token = cts.Token;

    public async Task Completion()
    {
        await fileService.Completion();
        await xmlFileBuildService.Completion();
    }

    public async Task ScanAsync(IEnumerable<XmlFileInfo> files)
    {
        foreach (var file in files)
        {
            if (token.IsCancellationRequested) return;
            await fileService.SendAsync(file);
        }
    }
}