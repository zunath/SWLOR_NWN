using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hook of the game's <c>Native/PlayerCreation</c>: <c>CServerExoAppInternal::ValidateCreateServerCharacter</c>.</summary>
internal static unsafe class PlayerCreationTrampolines
{
    internal const string ValidateSymbol = "_ZN21CServerExoAppInternal29ValidateCreateServerCharacterEP10CNWSPlayerPvj";

    private static nint _validateFunction;
    private static FunctionHook* _creationHook;
    private static volatile IPlayerCreationSink? _sink;

    internal static bool IsEnabled => _creationHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_validateFunction == 0)
            _validateFunction = native.GetExport(ValidateSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_creationHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void*, uint, uint> validate = &Validate;
        _creationHook = native.RequestHook(_validateFunction, (nint)validate, HookOrder.Early);
    }

    internal static IPlayerCreationSink? Sink => _sink;

    internal static void Bind(IPlayerCreationSink sink) => _sink = sink;

    internal static void Unbind(IPlayerCreationSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static uint Validate(void* server, void* client, void* data, uint size)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, void*, uint, uint>)_creationHook->m_trampoline)(server, client, data, size)
            : sink.Validate(server, client, data, size, (nint)_creationHook->m_trampoline);
    }
}
