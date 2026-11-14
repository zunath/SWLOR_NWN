using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Enables the hook that lets <see cref="PlayerCreationSink"/> authorize initial setup only after the engine
    /// accepts a new server character.
    /// </summary>
    public static class PlayerCreation
    {
        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.PlayerCreation);
        }
    }
}
