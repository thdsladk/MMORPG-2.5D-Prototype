// 역할 : 로그를 실제로 출력하는 대상. 서버는 콘솔, 클라이언트는 UnityEngine.Debug 로 구현한다.
namespace Game.Shared.Logging
{
    public interface ILogSink
    {
        void Write(LogLevel level, string message);
    }
}
