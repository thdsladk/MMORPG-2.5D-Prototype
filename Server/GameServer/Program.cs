// 역할 : 게임 서버 진입점. 로그·설정을 준비하고 서버를 띄운다 (네트워크·Zone 은 Phase 2~3 에서 추가).
using Game.GameServer.Config;
using Game.ServerCommon.Config;
using Game.ServerCommon.Logging;
using Game.Shared.Logging;

namespace Game.GameServer;

internal static class Program
{
    private const string ConfigFileName = "serverconfig.json";

    private static int Main()
    {
        // 1. 설정을 읽기 전에도 로그가 나오도록 콘솔 출력부터 연결
        Log.SetSink(new ConsoleLogSink());

        // 2. 설정 로드·검증
        ServerConfig config = JsonConfigLoader.Load<ServerConfig>(ConfigFileName);
        Log.SetSink(new ConsoleLogSink(), config.MinLogLevel);
        List<string> errors = config.Validate().ToList();
        if (errors.Count > 0)
        {
            errors.ForEach(Log.Error);
            return 1;
        }

        Log.Info($"GameServer 시작 — Port {config.Port}, TickRate {config.TickRate} ({config.TickIntervalMs:0.##}ms), MaxSessions {config.MaxSessions}");
        Log.Debug($"설정:\n{JsonConfigLoader.ToJson(config)}");

        // TODO: Phase 2 — Listener / Session, Phase 3 — Zone 틱 루프
        Log.Info("아직 네트워크가 없어 바로 종료합니다.");
        return 0;
    }
}
