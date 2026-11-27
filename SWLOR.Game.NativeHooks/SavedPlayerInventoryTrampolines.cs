using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hook of the game's <c>Native/SavedPlayerInventory</c>: <c>CNWSItem::CompareItem</c>. How many player files the
/// thread is loading, which the sink reads, is <see cref="PlayerFileLoadDepth"/>.
/// </summary>
internal static unsafe class SavedPlayerInventoryTrampolines
{
    internal const string CompareSymbol = "_ZN8CNWSItem11CompareItemEPS_";

    private static nint _compareFunction;
    private static FunctionHook* _compareHook;
    private static volatile ISavedPlayerInventorySink? _sink;

    internal static bool IsEnabled => _compareHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_compareFunction == 0)
            _compareFunction = native.GetExport(CompareSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_compareHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, int> compare = &Compare;
        _compareHook = native.RequestHook(_compareFunction, (nint)compare, HookOrder.Early);
    }

    internal static ISavedPlayerInventorySink? Sink => _sink;

    internal static void Bind(ISavedPlayerInventorySink sink) => _sink = sink;

    internal static void Unbind(ISavedPlayerInventorySink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int Compare(void* item, void* other)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, int>)_compareHook->m_trampoline)(item, other)
            : sink.CompareItem(item, other, (nint)_compareHook->m_trampoline);
    }
}
