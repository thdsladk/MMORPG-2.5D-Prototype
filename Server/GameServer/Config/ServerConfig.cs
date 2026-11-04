// 역할 : 게임 서버 설정 (serverconfig.json). 값의 의미와 기본값을 한곳에서 관리한다.
using System.Text.Json.Serialization;
using Game.Shared.Logging;

namespace Game.GameServer.Config;

public sealed class ServerConfig
{
    // 클라이언트가 접속하는 TCP 포트
    public int Port { get; set; } = 7777;

    // 동시에 받을 최대 세션 수 (부하 테스트 기준 500)
    public int MaxSessions { get; set; } = 500;

    // Zone 로직 틱 횟수 (초당). 20 = 50ms
    public int TickRate { get; set; } = 20;

    // 이 단계보다 낮은 로그는 출력하지 않는다
    public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;

    [JsonIgnore]
    public double TickIntervalMs => 1000.0 / TickRate;

    public IEnumerable<string> Validate()
    {
        if (Port is <= 0 or > 65535)
        {
            yield return $"Port 범위 오류: {Port}";
        }

        if (TickRate is < 1 or > 128)
        {
            yield return $"TickRate 범위 오류: {TickRate}";
        }

        if (MaxSessions < 1)
        {
            yield return $"MaxSessions 오류: {MaxSessions}";
        }
    }
}
