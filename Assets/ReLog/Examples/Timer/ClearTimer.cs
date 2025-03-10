using UdonSharp;

namespace ReLog.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ClearTimer : UdonSharpBehaviour
    {
        public CoreLogger Logger;
        public float Seconds = 60;

        public void _ClearLogs()
        {
            Logger.ClearEverything();
            SendEvent();
        }

        private void SendEvent() => SendCustomEventDelayedSeconds(nameof(_ClearLogs), Seconds);

        private void Start() => SendEvent();
    }
}