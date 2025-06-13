using Microsoft.Extensions.Logging;

namespace ZhApi.Bases;

/// <summary>
/// ❌⚠️⛔🚫✅✔️ 🔁🐞 ➡️ℹ️
/// </summary>
public abstract class LoggerBase()
    : ChannelTaskBase<string>(default), ILogger, IDisposable
{
    public virtual async void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var msg = formatter(state, exception);
        await SendAsync($"""
        {GetSymbol(logLevel)}
        {GetImg(logLevel)}{logLevel}: [{DateTime.Now}]
        {msg}


        """);
    }

    public abstract bool IsEnabled(LogLevel logLevel);

    public virtual IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => this;


    private static string GetImg(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Information or
        LogLevel.Trace or
        LogLevel.Debug or
        LogLevel.None => "",
        LogLevel.Warning => "⚠️ ",
        LogLevel.Error => "❌ ",
        LogLevel.Critical => "⛔ ",
        _ => ""
    };

    private static readonly string[] symbols =
    [
        new string('-',50),
        new string('!',50),
        new string('@',50),
    ];

    private static string GetSymbol(LogLevel logLevel) => logLevel switch
    {
        //LogLevel.Warning => symbols[1],
        //LogLevel.ErrorList => symbols[2],
        //LogLevel.Critical => symbols[2],
        _ => symbols[0],
    };
}