using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hook of the game's <c>Native/PlayerSlotAssignment</c>: <c>CNetLayerInternal::SetSlidingWindow</c>.</summary>
internal static unsafe class PlayerSlotAssignmentTrampolines
{
    internal const string SetSlidingWindowSymbol = "_ZN17CNetLayerInternal16SetSlidingWindowE16CNetConnectionIdPj";

    private static nint _setSlidingWindowFunction;
    private static FunctionHook* _hook;
    private static volatile IPlayerSlotAssignmentSink? _sink;

    internal static bool IsEnabled => _hook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_setSlidingWindowFunction == 0)
            _setSlidingWindowFunction = native.GetExport(SetSlidingWindowSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_hook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, uint, uint*, int> setSlidingWindow = &SetSlidingWindow;
        _hook = native.RequestHook(_setSlidingWindowFunction, (nint)setSlidingWindow, HookOrder.Earliest);
    }

    internal static IPlayerSlotAssignmentSink? Sink => _sink;

    internal static void Bind(IPlayerSlotAssignmentSink sink) => _sink = sink;

    internal static void Unbind(IPlayerSlotAssignmentSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int SetSlidingWindow(void* netLayer, uint connectionId, uint* windowId)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, uint, uint*, int>)_hook->m_trampoline)(netLayer, connectionId, windowId)
            : sink.SetSlidingWindow(netLayer, connectionId, windowId, (nint)_hook->m_trampoline);
    }
}
