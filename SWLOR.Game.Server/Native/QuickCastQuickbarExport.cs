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

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_saveHook != null)
            return;
        delegate* unmanaged<void*, void*, void*, void> save = &Save;
        _saveHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature16SaveQuickButtonsEP7CResGFFP10CResStruct"), (IntPtr)save, HookOrder.Early);
    }

    [UnmanagedCallersOnly]
    private static void Save(void* creature, void* resource, void* structure)
    {
        var restore = new List<CNWSQuickbarButton>();
        try
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
            var original = (delegate* unmanaged<void*, void*, void*, void>)_saveHook->m_trampoline;
            original(creature, resource, structure);
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Quick-cast quickbar export failed");
        }
        finally
        {
            foreach (var button in restore)
                button.m_nObjectType = (byte)QuickBarSlotType.Command;
        }
    }
}
