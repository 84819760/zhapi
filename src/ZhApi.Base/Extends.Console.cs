namespace ZhApi;

partial class Extends
{
    public static void WriteLineColor(this string value,
        ConsoleColor foreground = ConsoleColor.Gray, 
        ConsoleColor background = ConsoleColor.Black)
    {
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;

        Console.Write(value);
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void WriteColor(this string value,
    ConsoleColor foreground = ConsoleColor.Gray,
    ConsoleColor background = ConsoleColor.Black)
    {
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;

        Console.Write(value);
        Console.ResetColor();
    }
}


//public readonly record struct ConsoleColorHelper
//{
//    public ConsoleColorHelper() { }

//    public ConsoleColor Foreground { get; init; } = Console.ForegroundColor;

//    public ConsoleColor Background { get; init; } = Console.BackgroundColor;

//    public readonly void WriteLine(string value)
//    {
//        Console.ForegroundColor = Foreground;
//        Console.BackgroundColor = Background;

//        Console.WriteNode(value);
//        Console.ResetColor();
//        Console.WriteLine();
//    }
//}
