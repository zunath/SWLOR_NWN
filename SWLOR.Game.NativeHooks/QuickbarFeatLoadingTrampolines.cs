using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hook of the game's <c>Native/QuickbarFeatLoading</c>: <c>CNWSCreature::LoadQuickButtons</c>.</summary>
internal static unsafe class QuickbarFeatLoadingTrampolines
{
    internal const string LoadSymbol = "_ZN12CNWSCreature16LoadQuickButtonsEP7CResGFFP10CResStruct";

    private static nint _loadFunction;
    private static FunctionHook* _loadHook;
    private static volatile IQuickbarFeatLoadingSink? _sink;

    internal static bool IsEnabled => _loadHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_loadFunction == 0)
            _loadFunction = native.GetExport(LoadSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_loadHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void*, void> load = &Load;
        _loadHook = native.RequestHook(_loadFunction, (nint)load, HookOrder.Early);
    }

    internal static IQuickbarFeatLoadingSink? Sink => _sink;

    internal static void Bind(IQuickbarFeatLoadingSink sink) => _sink = sink;

    internal static void Unbind(IQuickbarFeatLoadingSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static void Load(void* creature, void* resource, void* structure)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, void*, void*, void>)_loadHook->m_trampoline)(creature, resource, structure);
        else
            sink.LoadQuickButtons(creature, resource, structure, (nint)_loadHook->m_trampoline);
    }
}
