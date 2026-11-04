// 역할 : 공용 코드에서 쓰는 정적 로그 진입점. 실행 환경(서버/클라이언트)이 시작할 때 SetSink 로 출력 대상을 정한다.
//        출력 대상이 없으면 아무것도 하지 않으므로, 공용 코드는 어느 쪽에서 실행돼도 안전하다.
namespace Game.Shared.Logging
{
    public static class Log
    {
        private static ILogSink s_sink;
        private static LogLevel s_minLevel = LogLevel.Debug;

        public static LogLevel MinLevel => s_minLevel;

        public static void SetSink(ILogSink sink, LogLevel minLevel = LogLevel.Debug)
        {
            s_sink = sink;
            s_minLevel = minLevel;
        }

        public static void Debug(string message) => Write(LogLevel.Debug, message);

        public static void Info(string message) => Write(LogLevel.Info, message);

        public static void Warning(string message) => Write(LogLevel.Warning, message);

        public static void Error(string message) => Write(LogLevel.Error, message);

        private static void Write(LogLevel level, string message)
        {
            if (s_sink == null || level < s_minLevel)
            {
                return;
            }

            s_sink.Write(level, message);
        }
    }
}
