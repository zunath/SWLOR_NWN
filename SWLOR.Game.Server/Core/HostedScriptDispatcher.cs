using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The run-script callback of a game build the host boots: the body of the native entry's
    /// <c>NativeInteropManager.OnRunScript</c> after it has read the script name, with its result split the way the
    /// package carries it. <see cref="ScriptExecutor.ProcessRunScript"/> answers -1 (not handled), 0 (handled, or a false
    /// conditional) or 1 (a true conditional); the host turns "handled" and the conditional result back into the same
    /// number for the engine (<c>ReloadHost.DispatchScriptResult</c>), so a conversation's "appears when" check keeps its
    /// answer.
    /// </summary>
    public sealed class HostedScriptDispatcher : IScriptDispatcher
    {
        private const int ScriptNotHandled = -1;
        private const int ConditionTrue = 1;

        private readonly Func<string, uint, int> _processRunScript;

        /// <summary>Dispatches to the server's <see cref="ScriptExecutor"/>.</summary>
        public HostedScriptDispatcher()
            : this((script, objectSelf) => ServerManager.Executor.ProcessRunScript(script, objectSelf))
        {
        }

        /// <summary>For tests: <paramref name="processRunScript"/> stands in for <see cref="ScriptExecutor.ProcessRunScript"/>.</summary>
        public HostedScriptDispatcher(Func<string, uint, int> processRunScript)
        {
            _processRunScript = processRunScript ?? throw new ArgumentNullException(nameof(processRunScript));
        }

        /// <inheritdoc/>
        public bool TryHandle(string scriptName, uint objectSelf, out bool conditionalResult)
        {
            var result = RunScript(scriptName, objectSelf);
            conditionalResult = result == ConditionTrue;
            return result != ScriptNotHandled;
        }

        private int RunScript(string scriptName, uint objectSelf)
        {
            var entered = ManagedEntryDepth.Enter();
            try
            {
                return _processRunScript(scriptName, objectSelf);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"RunScript exception: {e}", true);
                return ScriptNotHandled;
            }
            finally
            {
                ManagedEntryDepth.Exit(entered);
            }
        }
    }
}
