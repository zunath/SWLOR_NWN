using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native;

/// <summary>Saved quickbars may contain either targeting representation of an ability.</summary>
public static unsafe class QuickbarFeatLoading
{
    private static FunctionHook* _loadHook;

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_loadHook != null)
            return;
        delegate* unmanaged<void*, void*, void*, void> load = &Load;
        _loadHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature16LoadQuickButtonsEP7CResGFFP10CResStruct"), (IntPtr)load, HookOrder.Early);
    }

    [UnmanagedCallersOnly]
    private static void Load(void* creature, void* resource, void* structure)
    {
        var original = (delegate* unmanaged<void*, void*, void*, void>)_loadHook->m_trampoline;
        original(creature, resource, structure);
        try
        {
            var id = CNWSCreature.FromPointer(creature).m_idSelf;
            ServerManager.Executor.ExecuteInScriptContext(() => QuickCast.ApplyPreferences(id), id);
        }
        catch (Exception exception) { Log.WriteError(exception, "Ability targeting quickbar restoration failed"); }
    }
}
