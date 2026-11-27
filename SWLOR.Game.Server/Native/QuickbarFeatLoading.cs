using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native;

/// <summary>Enables the hook that lets <see cref="QuickbarFeatLoadingSink"/> restore saved quickbars with either targeting representation of an ability.</summary>
public static class QuickbarFeatLoading
{
    [BootPhase(BootReload.NativeHook)]
    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        NativeHookAccess.Hooks.Enable(NativeHookId.QuickbarFeatLoading);
    }
}
