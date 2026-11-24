using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hooks of the game's <c>Native/ClientJsonUtf8Guard</c>: <c>CNWMessage::ReadJSON</c> and <c>CNWSMessage::HandlePlayerToServerNuiEvent</c>.</summary>
internal static unsafe class ClientJsonUtf8GuardTrampolines
{
    internal const string ReadJsonSymbol = "_ZN10CNWMessage8ReadJSONB5cxx11Ev";
    internal const string NuiEventSymbol = "_ZN11CNWSMessage28HandlePlayerToServerNuiEventEP10CNWSPlayerh";

    private static nint _readJsonFunction;
    private static nint _nuiEventFunction;
    private static FunctionHook* _readJsonHook;
    private static FunctionHook* _nuiEventHook;
    private static volatile IClientJsonUtf8GuardSink? _sink;

    internal static bool IsEnabled => _readJsonHook != null && _nuiEventHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_readJsonFunction == 0)
            _readJsonFunction = native.GetExport(ReadJsonSymbol);
        if (_nuiEventFunction == 0)
            _nuiEventFunction = native.GetExport(NuiEventSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        Resolve(native);
        if (_readJsonHook == null)
        {
            delegate* unmanaged<void*, void*, void*> readJson = &ReadJson;
            _readJsonHook = native.RequestHook(_readJsonFunction, (nint)readJson, HookOrder.Earliest);
        }

        if (_nuiEventHook == null)
        {
            delegate* unmanaged<void*, void*, byte, int> nuiEvent = &HandleNuiEvent;
            _nuiEventHook = native.RequestHook(_nuiEventFunction, (nint)nuiEvent, HookOrder.Earliest);
        }
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

    // Every NUI event and bind update a client sends passes through here before its JSON is read.
    [UnmanagedCallersOnly]
    private static int HandleNuiEvent(void* message, void* player, byte minor)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, byte, int>)_nuiEventHook->m_trampoline)(message, player, minor)
            : sink.HandleNuiEvent(message, player, minor, (nint)_nuiEventHook->m_trampoline);
    }
}
