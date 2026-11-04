// 역할 : 연결 확인·RTT 측정용 패킷 (Phase 2-7 에서 사용).
using MemoryPack;

namespace Game.Shared.Packets
{
    [MemoryPackable]
    public partial class CS_Ping
    {
        // 클라이언트가 보낸 시각 (밀리초). 서버는 그대로 돌려준다.
        public long ClientTimeMs { get; set; }
    }

    [MemoryPackable]
    public partial class SC_Pong
    {
        public long ClientTimeMs { get; set; }

        public long ServerTimeMs { get; set; }
    }
}
