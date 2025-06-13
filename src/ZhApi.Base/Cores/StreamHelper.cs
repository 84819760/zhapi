namespace ZhApi.Cores;
public class StreamHelper(string filePath, CancellationToken token = default) : Stream
{
    private readonly static Lock @lock = new();
    private FileStream fs = null!;
    private bool isFirst = true;

    protected override void Dispose(bool disposing)
    {
        fs?.Dispose();
        base.Dispose(disposing);
    }

    #region MyRegion
    public override void Flush() =>
        throw new NotImplementedException();

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotImplementedException();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek { get; }
    public override long Length { get; }
    public override long Position { get; set; }
    #endregion

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!token.IsCancellationRequested)
            GetAction()(buffer, offset, count);
    }

    private Action<byte[], int, int> GetAction()
    {
        lock (@lock)
        {
            if (!isFirst) return WriteFile;

            isFirst = false;
            return FirstWrite;
        }
    }

    void FirstWrite(byte[] buffer, int offset, int count)
    {
        isFirst = false;
        if (count < 3) return;
        File.WriteAllText(filePath, "");
        fs = File.OpenWrite(filePath);
        fs.Write(buffer, offset, count);
    }

    void WriteFile(byte[] buffer, int offset, int count) =>
        fs.Write(buffer, offset, count);
}