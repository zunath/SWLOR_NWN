using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Core
{
    public class MainLoopProcessor
    {
        public static event Action OnScriptContextBegin;
        public static event Action OnScriptContextEnd;

        public void ProcessMainLoop(ulong frame)
        {
            FrameProfiler.OnFrameStart(frame);
            var profile = FrameProfiler.EnterManaged();
            OnScriptContextBegin?.Invoke();

            try
            {
                ProcessAsynchronousTasks();
                ProcessScheduledTasks();
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, ex.ToMessageAndCompleteStacktrace(), true);
            }

            OnScriptContextEnd?.Invoke();
            FrameProfiler.ExitManaged(FrameMetric.Scheduler, profile);
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
