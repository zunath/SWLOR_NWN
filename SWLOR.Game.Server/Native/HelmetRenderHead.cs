using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// Keeps a helmet that renders through the head out of the replicated appearance.
/// CNWSCreature::UpdateAppearanceForEquippedItems rebuilds the replicated equipment from the
/// inventory before every client appearance update, which would otherwise restore the native
/// helmet attachment over the tinted render head.
/// </summary>
public static unsafe class HelmetRenderHead
{
    private static FunctionHook* _updateAppearanceHook;

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_updateAppearanceHook != null) return;
        delegate* unmanaged<void*, void> update = &UpdateAppearanceForEquippedItems;
        _updateAppearanceHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature32UpdateAppearanceForEquippedItemsEv"),
            (IntPtr)update, HookOrder.Late);
    }

    [UnmanagedCallersOnly]
    private static void UpdateAppearanceForEquippedItems(void* creature)
    {
        var original = (delegate* unmanaged<void*, void>)_updateAppearanceHook->m_trampoline;
        original(creature);
        try
        {
            var nativeCreature = CNWSCreature.FromPointer(creature);
            if (HelmetModelRenderer.IsProjected(nativeCreature.m_idSelf))
                nativeCreature.m_cAppearance.m_oidHeadItem = OBJECT_INVALID;
        }
        catch (Exception ex)
        {
            Log.WriteError(ex, "Could not keep a tinted helmet on its render head");
        }
    }
}
