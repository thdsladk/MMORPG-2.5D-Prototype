// 역할 : 패킷 종류 번호. 헤더의 [ushort id] 에 들어간다.
//        CS_ = 클라이언트 → 서버, SC_ = 서버 → 클라이언트. 번호는 한 번 정하면 바꾸지 않는다.
//        (S_ 접두어는 .NET 네이밍 검사가 static 필드 접두어 s_ 로 보고 막아서 쓰지 않는다)
namespace Game.Shared.Packets
{
    public enum PacketId : ushort
    {
        None = 0,

        // 1~99: 연결·진단
        CS_Ping = 1,
        SC_Pong = 2,
    }
}
