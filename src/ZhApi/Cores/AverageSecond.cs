namespace ZhApi.WpfApp.Cores;
#if true
public class AverageSecond
{
    private static readonly Lock @lock = new();
    private double second;
    private int count;

    public int Count => count;

    public double GetValue(TimeSpan time)
    {
        lock (@lock)
        {
            second += time.TotalSeconds;
            count += 1;
            return second / count;
        }
    } 
}
#else 
public class AverageSecond(int max = 100)
{
    private static readonly Lock @lock = new();
    private readonly List<double> times = [];

    public int Count => times.Count;

    public double GetValue(TimeSpan time)
    {
        lock (@lock)
        {
            times.Add(time.TotalSeconds);

            if(times.Count > max)
                times.RemoveAt(0);

            return times.Average();
        }
    }
}
#endif
