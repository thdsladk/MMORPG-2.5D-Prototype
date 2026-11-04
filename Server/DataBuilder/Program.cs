// 역할 : DataBuilder 진입점 (기능은 이후 페이즈에서 추가: DummyClient = Phase 2-9 부하 봇, DataBuilder = Phase 6 테이블 빌드).
using Game.ServerCommon.Logging;
using Game.Shared.Logging;

namespace Game.DataBuilder;

internal static class Program
{
    private static int Main()
    {
        Log.SetSink(new ConsoleLogSink());
        Log.Info("DataBuilder — 아직 구현 전입니다.");
        return 0;
    }
}
