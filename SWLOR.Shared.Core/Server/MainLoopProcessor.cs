using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log;

namespace SWLOR.Shared.Core.Server
{
    public class MainLoopProcessor
    {
        public static event Action OnScriptContextBegin;
        public static event Action OnScriptContextEnd;

        public void ProcessMainLoop(ulong frame)
        {
            OnScriptContextBegin?.Invoke();

            try
            {
                ProcessAsynchronousTasks();
                ProcessScheduledTasks();
            }
            catch (Exception ex)
            {
                Log.Log.Write(LogGroup.Error, ex.ToMessageAndCompleteStacktrace(), true);
            }

            OnScriptContextEnd?.Invoke();
        }

        private void ProcessAsynchronousTasks()
        {
            NwTask.MainThreadSynchronizationContext.Update();
        }

        private void ProcessScheduledTasks()
        {
            Scheduler.Process();
        }
    }
}