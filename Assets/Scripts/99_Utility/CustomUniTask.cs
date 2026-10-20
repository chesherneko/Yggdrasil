using System.Threading;
using Cysharp.Threading.Tasks;

namespace Utility
{
    /// <summary>
    /// float로 몇 초인지를 매개변수로 보내면 int milliseconds로 변환하여 UniTask.Delay를 실행하는 커스텀 메서드
    /// 기본적으로 DelayType은 DelayType.DeltaTime이고, 그 이외는 UniTask.Delay와 동일하게 돌아감
    /// </summary>
    public static class CustomUniTask
    {
        public static UniTask DelaySeconds(float secondsDelay, DelayType delayType = DelayType.DeltaTime, 
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update, 
            CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
        {
            int milliseconds = (int)(secondsDelay * 1000);
            return UniTask.Delay(milliseconds, delayType, delayTiming, cancellationToken, cancelImmediately);
        }
    }
}