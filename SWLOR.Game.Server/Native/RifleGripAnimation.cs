using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Native;

/// <summary>Keeps the rifle grip seated when the native hold changes.</summary>
public static unsafe class RifleGripAnimation
{
    private static FunctionHook* _hook;
    private static bool _refreshing;

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_hook != null) return;
        delegate* unmanaged<void*, int, void> set = &SetAnimation;
        _hook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature12SetAnimationEi"), (IntPtr)set, HookOrder.Late);
    }

    [UnmanagedCallersOnly]
    private static void SetAnimation(void* pointer, int animation)
    {
        var original = (delegate* unmanaged<void*, int, void>)_hook->m_trampoline;
        original(pointer, animation);
        if (_refreshing) return;
        try
        {
            var creature = CNWSCreature.FromPointer(pointer);
            // The appearance cache can lag behind equipment on an unseen creature.
            var item = creature.m_pInventory?.GetItemInSlot((uint)EquipmentSlot.RightHand);
            if (item == null || (BaseItem)item.m_nBaseItem != BaseItem.Rifle) return;
            _refreshing = true;
            // Native playback can reject or coerce the requested hold. Seat the
            // weapon against the animation the engine actually retained.
            var actualAnimation = creature.m_nAnimation;
            ServerManager.Executor.ExecuteInScriptContext(() =>
                WeaponFit.ApplyRifleAnimation(creature.m_idSelf, actualAnimation));
        }
        catch (Exception exception) { Log.WriteError(exception, "Could not refresh the rifle grip hold"); }
        finally { _refreshing = false; }
    }
}
