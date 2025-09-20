using System;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Shared.Core.Server
{
    public class MainLoopProcessor : IMainLoopProcessor
    {
        private readonly ILogger _logger;
        
        public event Action OnScriptContextBegin;
        public event Action OnScriptContextEnd;

        public MainLoopProcessor(ILogger logger)
        {
            _logger = logger;
        }

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
                _logger.Write<ErrorLogGroup>(ex.ToMessageAndCompleteStacktrace(), true);
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