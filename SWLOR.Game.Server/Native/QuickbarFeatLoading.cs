using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native;

/// <summary>Installs the hook that lets <see cref="QuickbarFeatLoadingSink"/> restore saved quickbars with either targeting representation of an ability.</summary>
public static unsafe class QuickbarFeatLoading
{
    private static FunctionHook* _loadHook;
    private static IQuickbarFeatLoadingSink _sink;

    [BootPhase(BootReload.NativeHook)]
    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHook()
    {
        if (_loadHook != null)
            return;
        _sink ??= new QuickbarFeatLoadingSink();
        delegate* unmanaged<void*, void*, void*, void> load = &Load;
        _loadHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
            "_ZN12CNWSCreature16LoadQuickButtonsEP7CResGFFP10CResStruct"), (IntPtr)load, HookOrder.Early);
    }

    [UnmanagedCallersOnly]
    private static void Load(void* creature, void* resource, void* structure)
        => _sink.LoadQuickButtons(creature, resource, structure, (nint)_loadHook->m_trampoline);
}
