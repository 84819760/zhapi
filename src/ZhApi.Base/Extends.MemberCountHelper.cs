using System.Buffers;

namespace ZhApi;
internal class MemberCountHelper
{
    private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
    private const string member = "<member";
    private const int length = 7;

    private readonly static SearchValues<string> searchValues =
           SearchValues.Create(["<member ", "<member/>", "<member>"], IgnoreCase);

    public static async Task<int> GetMemberCountAsync(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        using var fs = File.OpenText(filePath);
        return await GetMemberCount(fs);
    }

    private static async Task<int> GetMemberCount(StreamReader fs)
    {
        var count = 0;
        while (true)
        {
            var line = await fs.ReadLineAsync();
            if (line is null) break;
            GetMemberCount(line, ref count);
        }
        return count;
    }

    private static void GetMemberCount(ReadOnlySpan<char> span, ref int count)
    {
        while (true)
        {
            var index = span.IndexOfAny(searchValues);
            if (index > 0)
            {
                count += 1;
                span = span[(index + length)..];
            }
            else if (span.EndsWith(member, IgnoreCase))
            {
                count += 1;
                return;
            }
            else return;
        }
    }
}
