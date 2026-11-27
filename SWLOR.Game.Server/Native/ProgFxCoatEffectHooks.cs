using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Tells <see cref="Domain.Companions.IProgFxCoatService"/> when a coating visual effect or petrification is applied
    /// to or removed from a creature. NWNX's effect events skip visual effects, and a DM's
    /// ##dm_visualeffect applies one outside any script, so the engine's own handlers are observed
    /// (<c>SWLOR.Game.NativeHooks.ProgFxCoatEffectHooksTrampolines</c>, forwarding to <c>ProgFxCoatEffectHooksSink</c>).
    /// </summary>
    public static class ProgFxCoatEffectHooks
    {
        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.ProgFxCoatEffectHooks);
        }
    }
}
