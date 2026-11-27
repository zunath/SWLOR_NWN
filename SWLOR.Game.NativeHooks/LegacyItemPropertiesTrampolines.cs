using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hook of the game's <c>Native/LegacyItemProperties</c>: <c>CNWSItem::LoadItem</c>, enabled at module preload.</summary>
internal static unsafe class LegacyItemPropertiesTrampolines
{
    internal const string LoadItemSymbol = "_ZN8CNWSItem8LoadItemEP7CResGFFP10CResStructii";

    private static nint _loadItemFunction;
    private static FunctionHook* _loadItemHook;
    private static volatile ILegacyItemPropertiesSink? _sink;

    internal static bool IsEnabled => _loadItemHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_loadItemFunction == 0)
            _loadItemFunction = native.GetExport(LoadItemSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_loadItemHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void*, int, int, int> loadItem = &LoadItem;
        _loadItemHook = native.RequestHook(_loadItemFunction, (nint)loadItem, HookOrder.Early);
    }

    internal static ILegacyItemPropertiesSink? Sink => _sink;

    internal static void Bind(ILegacyItemPropertiesSink sink) => _sink = sink;

    internal static void Unbind(ILegacyItemPropertiesSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int LoadItem(void* item, void* resource, void* structure, int firstFlag, int secondFlag)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, void*, int, int, int>)_loadItemHook->m_trampoline)(item, resource, structure, firstFlag, secondFlag)
            : sink.LoadItem(item, resource, structure, firstFlag, secondFlag, (nint)_loadItemHook->m_trampoline);
    }
}
