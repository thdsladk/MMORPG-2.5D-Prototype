// 역할 : 공용 로그를 콘솔에 "시각 [단계] 메시지" 형식으로 출력한다. 여러 스레드에서 불려도 줄이 섞이지 않게 잠근다.
using Game.Shared.Logging;

namespace Game.ServerCommon.Logging;

public sealed class ConsoleLogSink : ILogSink
{
    private readonly object _lock = new();

    public void Write(LogLevel level, string message)
    {
        string line = $"{DateTime.Now:HH:mm:ss.fff} [{ToTag(level)}] {message}";
        lock (_lock)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ToColor(level);
            Console.WriteLine(line);
            Console.ForegroundColor = previous;
        }
    }

    private static string ToTag(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "???",
    };

    private static ConsoleColor ToColor(LogLevel level) => level switch
    {
        LogLevel.Debug => ConsoleColor.DarkGray,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        _ => ConsoleColor.Gray,
    };
}
