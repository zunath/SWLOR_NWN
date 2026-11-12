using Nwn.Hosting.Contracts.Generations;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// A script dispatcher that hands every script to the server's and, the first time the module's load script
    /// (<see cref="ScriptName.OnModuleLoad"/>) has run, calls <c>onModuleLoaded</c>: the moment every boot script has run. NWNX
    /// signals the module load before it, so the signal is too early for what the boot built. Used only for the boot of a server
    /// with hot reload on, to take the boot's reload digests.
    /// </summary>
    public sealed class ModuleLoadedScriptDispatcher : IScriptDispatcher
    {
        private readonly IScriptDispatcher _inner;
        private Action _onModuleLoaded;

        public ModuleLoadedScriptDispatcher(IScriptDispatcher inner, Action onModuleLoaded)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _onModuleLoaded = onModuleLoaded ?? throw new ArgumentNullException(nameof(onModuleLoaded));
        }

        /// <inheritdoc/>
        public bool TryHandle(string scriptName, uint objectSelf, out bool conditionalResult)
        {
            var handled = _inner.TryHandle(scriptName, objectSelf, out conditionalResult);
            if (scriptName == ScriptName.OnModuleLoad && _onModuleLoaded != null)
            {
                var onModuleLoaded = _onModuleLoaded;
                _onModuleLoaded = null;
                onModuleLoaded();
            }

            return handled;
        }
    }
}
