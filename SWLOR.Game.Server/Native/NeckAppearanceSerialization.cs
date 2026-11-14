using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Enables the hook that lets <see cref="NeckAppearanceSerializationSink"/> project the equipped neck, shoulder and part-skin models into the appearance the engine sends.</summary>
    public static class NeckAppearanceSerialization
    {
        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.NeckAppearanceSerialization);
        }
    }
}
