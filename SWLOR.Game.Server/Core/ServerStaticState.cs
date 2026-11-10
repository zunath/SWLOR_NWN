using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Core
{
    /// <inheritdoc cref="IServerStaticState"/>
    public sealed class ServerStaticState : IServerStaticState
    {
        public void ClearScheduler()
        {
            Scheduler.Clear();
        }

        public void ClearNwTaskQueue()
        {
            NwTask.MainThreadSynchronizationContext.Clear();
        }

        public void FlushLogs()
        {
            Log.OnApplicationShutdown();
        }

        public void ClearScriptExecutionProvider()
        {
            ScriptExecutionProvider.SetProvider(null);
        }
    }
}
