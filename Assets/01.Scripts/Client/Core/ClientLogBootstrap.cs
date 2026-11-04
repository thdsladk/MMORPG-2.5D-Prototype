// 역할 : 게임 시작(씬 로드 전)에 공용 로그 출력 대상을 Unity 콘솔로 연결한다.
//        개발 빌드·에디터는 Debug 단계부터, 릴리스 빌드는 Info 단계부터 출력한다.
using Game.Shared.Logging;
using UnityEngine;

namespace Game.Client.Core
{
    public static class ClientLogBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            LogLevel minLevel = Debug.isDebugBuild ? LogLevel.Debug : LogLevel.Info;
            Log.SetSink(new UnityLogSink(), minLevel);
        }
    }
}
