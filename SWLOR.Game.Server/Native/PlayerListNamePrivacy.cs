using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Enables the hook that lets <see cref="PlayerListNamePrivacySink"/> serialize player-list additions separately for each observer's private names.</summary>
    public static class PlayerListNamePrivacy
    {
        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.PlayerListNamePrivacy);
        }
    }
}
