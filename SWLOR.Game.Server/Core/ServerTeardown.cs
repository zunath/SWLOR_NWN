using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Core
{
    /// <inheritdoc cref="IServerTeardown"/>
    /// <remarks>
    /// The <c>app_shutdown</c> script still runs its two handlers (<c>CombatRecorderService.StopOnShutdown</c> and
    /// <c>Log.OnApplicationShutdown</c>) in that order, so the shutdown the engine triggers is unchanged; this class runs
    /// the same two steps in the same order, surrounded by the steps only a retiring generation needs.
    /// </remarks>
    public sealed class ServerTeardown : IServerTeardown
    {
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IProcessEventSubscriptions _processEvents;
        private readonly IServerStaticState _staticState;

        public ServerTeardown(
            ICombatRecorderService combatRecorder,
            IProcessEventSubscriptions processEvents,
            IServerStaticState staticState)
        {
            _combatRecorder = combatRecorder ?? throw new ArgumentNullException(nameof(combatRecorder));
            _processEvents = processEvents ?? throw new ArgumentNullException(nameof(processEvents));
            _staticState = staticState ?? throw new ArgumentNullException(nameof(staticState));
        }

        public void Run()
        {
            var failures = new List<Exception>();

            Step(_staticState.ClearScheduler, failures);
            Step(_staticState.ClearNwTaskQueue, failures);

            // The app_shutdown clean-up, in the order of the handlers in ScriptHandlerOrder.txt.
            Step(_combatRecorder.StopOnShutdown, failures);
            Step(_staticState.FlushLogs, failures);

            Step(_processEvents.UnsubscribeAll, failures);
            Step(_staticState.ClearScriptExecutionProvider, failures);

            // Last: nothing after it may ask System.ComponentModel about the build's types again.
            Step(_staticState.ReleaseFrameworkTypeCaches, failures);

            if (failures.Count == 1)
                throw failures[0];
            if (failures.Count > 1)
                throw new AggregateException("Server teardown steps failed.", failures);
        }

        private static void Step(Action step, List<Exception> failures)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
    }
}
