// 역할 : 공용 로그(Game.Shared.Logging.Log)를 Unity 콘솔로 출력한다.
using Game.Shared.Logging;
using UnityEngine;

namespace Game.Client.Core
{
    public sealed class UnityLogSink : ILogSink
    {
        public void Write(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
    }
}
