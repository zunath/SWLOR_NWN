using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hook of the game's <c>Native/NeckAppearanceSerialization</c>: <c>CNWSMessage::AddItemAppearanceToMessage</c>.</summary>
internal static unsafe class NeckAppearanceSerializationTrampolines
{
    internal const string WriteSymbol = "_ZN11CNWSMessage26AddItemAppearanceToMessageEP10CNWSPlayerP8CNWSItem";

    private static nint _writeFunction;
    private static FunctionHook* _hook;
    private static volatile INeckAppearanceSerializationSink? _sink;

    internal static bool IsEnabled => _hook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_writeFunction == 0)
            _writeFunction = native.GetExport(WriteSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_hook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void*, void> write = &Write;
        _hook = native.RequestHook(_writeFunction, (nint)write, HookOrder.Late);
    }

    internal static INeckAppearanceSerializationSink? Sink => _sink;

    internal static void Bind(INeckAppearanceSerializationSink sink) => _sink = sink;

    internal static void Unbind(INeckAppearanceSerializationSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static void Write(void* message, void* player, void* itemPointer)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, void*, void*, void>)_hook->m_trampoline)(message, player, itemPointer);
        else
            sink.Write(message, player, itemPointer, (nint)_hook->m_trampoline);
    }
}
