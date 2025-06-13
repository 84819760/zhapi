namespace ZhApi;
partial class Extends
{
    public static double UnNaN(this double v)
    {
        if (double.IsNaN(v)) return 0;
        return v;
    }

    public static double GetProgress(this double current, int total) =>
      Math.Round(current / total, 3).UnNaN();

    public static double GetProgress(this int current, int total) =>
        GetProgress((double)current, total);
}
