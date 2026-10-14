using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native;

/// <summary>Leaves login-only quick-cast shortcuts out of character exports.</summary>
public static unsafe class QuickCastCharacterExport
{
    private static FunctionHook* _saveHook;

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_saveHook != null)
            return;
        delegate* unmanaged<void*, void*, void*, int, int, int, int, int> save = &Save;
        _saveHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature12SaveCreatureEP7CResGFFP10CResStructiiii"), (IntPtr)save, HookOrder.Early);
    }

    [UnmanagedCallersOnly]
    private static int Save(void* creature, void* resource, void* structure,
        int associates, int desiredArea, int exporting, int objectIds)
    {
        var restore = new List<CNWSQuickbarButton>();
        try
        {
            var native = CNWSCreature.FromPointer(creature);
            if (exporting != 0)
            {
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
            }
            var original = (delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)_saveHook->m_trampoline;
            return original(creature, resource, structure, associates, desiredArea, exporting, objectIds);
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Quick-cast character export failed");
            return 0;
        }
        finally
        {
            foreach (var button in restore)
                button.m_nObjectType = (byte)QuickBarSlotType.Command;
        }
    }
}
