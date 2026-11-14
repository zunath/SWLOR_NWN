using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hook of the game's <c>Native/ClientJsonUtf8Guard</c>: <c>CNWMessage::ReadJSON</c>.</summary>
internal static unsafe class ClientJsonUtf8GuardTrampolines
{
    internal const string ReadJsonSymbol = "_ZN10CNWMessage8ReadJSONB5cxx11Ev";

    private static nint _readJsonFunction;
    private static FunctionHook* _readJsonHook;
    private static volatile IClientJsonUtf8GuardSink? _sink;

    internal static bool IsEnabled => _readJsonHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_readJsonFunction == 0)
            _readJsonFunction = native.GetExport(ReadJsonSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_readJsonHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void*> readJson = &ReadJson;
        _readJsonHook = native.RequestHook(_readJsonFunction, (nint)readJson, HookOrder.Earliest);
    }

    internal static IClientJsonUtf8GuardSink? Sink => _sink;

    internal static void Bind(IClientJsonUtf8GuardSink sink) => _sink = sink;

    internal static void Unbind(IClientJsonUtf8GuardSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    // ReadJSON returns the decoded json through a hidden result pointer, ahead of the message pointer.
    [UnmanagedCallersOnly]
    private static void* ReadJson(void* result, void* message)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, void*>)_readJsonHook->m_trampoline)(result, message)
            : sink.ReadJson(result, message, (nint)_readJsonHook->m_trampoline);
    }
}
