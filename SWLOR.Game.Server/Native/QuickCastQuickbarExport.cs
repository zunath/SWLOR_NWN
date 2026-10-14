using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native;

/// <summary>Leaves login-only quick-cast shortcuts out of saved quickbars.</summary>
public static unsafe class QuickCastQuickbarExport
{
    private static FunctionHook* _saveHook;
    private static FunctionHook* _loadHook;

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_saveHook != null)
            return;
        delegate* unmanaged<void*, void*, void*, void> save = &Save;
        _saveHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature16SaveQuickButtonsEP7CResGFFP10CResStruct"), (IntPtr)save, HookOrder.Early);
        delegate* unmanaged<void*, void*, void*, void> load = &Load;
        _loadHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature16LoadQuickButtonsEP7CResGFFP10CResStruct"), (IntPtr)load, HookOrder.Early);
    }

    public static void SaveWithTemporarySlots(Action mask, Action save, Action restore, Action<Exception> reportMaskFailure)
    {
        try
        {
            try { mask(); }
            catch (Exception exception) { reportMaskFailure(exception); }
        }
        finally
        {
            try { save(); }
            finally { restore(); }
        }
    }

    [UnmanagedCallersOnly]
    private static void Save(void* creature, void* resource, void* structure)
    {
        var restore = new List<CNWSQuickbarButton>();
        try
        {
            var original = (delegate* unmanaged<void*, void*, void*, void>)_saveHook->m_trampoline;
            SaveWithTemporarySlots(() =>
                {
                    var native = CNWSCreature.FromPointer(creature);
                    foreach (var binding in QuickCast.GetBoundCommandsForExport(native.m_idSelf))
                    {
                        var button = native.GetQuickbarButton((byte)binding.Slot);
                        if (button.m_nObjectType != (byte)QuickBarSlotType.Command ||
                            button.m_sCommandLine.ToString() != binding.Command)
                            continue;
                        restore.Add(button);
                        // Change only the native type for this synchronous save. No client
                        // quickbar updates are sent, and later manual edits are left intact.
                        button.m_nObjectType = (byte)QuickBarSlotType.Empty;
                    }
                },
                () => original(creature, resource, structure),
                () =>
                {
                    foreach (var button in restore)
                        button.m_nObjectType = (byte)QuickBarSlotType.Command;
                },
                exception => Log.WriteError(exception, "Quick-cast quickbar export masking failed"));
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Quick-cast quickbar export failed");
        }
    }

    [UnmanagedCallersOnly]
    private static void Load(void* creature, void* resource, void* structure)
    {
        try
        {
            // A different layout has its own shortcuts, even if a command happens to
            // match the old bar at the same position. Opt-in modes still last this login.
            QuickCast.ForgetBindings(CNWSCreature.FromPointer(creature).m_idSelf);
        }
        catch (Exception exception) { Log.WriteError(exception, "Quick-cast quickbar ownership reset failed"); }
        var original = (delegate* unmanaged<void*, void*, void*, void>)_loadHook->m_trampoline;
        original(creature, resource, structure);
    }
}
