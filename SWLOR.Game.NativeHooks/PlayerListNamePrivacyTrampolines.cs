using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>The hook of the game's <c>Native/PlayerListNamePrivacy</c>: <c>CNWSMessage::SendServerToPlayerPlayerList_Add</c>.</summary>
internal static unsafe class PlayerListNamePrivacyTrampolines
{
    internal const string AddSymbol = "_ZN11CNWSMessage32SendServerToPlayerPlayerList_AddEjP10CNWSPlayer";

    private static nint _addFunction;
    private static FunctionHook* _addHook;
    private static volatile IPlayerListNamePrivacySink? _sink;

    internal static bool IsEnabled => _addHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_addFunction == 0)
            _addFunction = native.GetExport(AddSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_addHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, uint, void*, int> add = &SendPlayerListAdd;
        // Run outside NWNX Rename's Early hook. Its broadcast path applies every
        // observer's name to one creature, then serializes only the last name once.
        _addHook = native.RequestHook(_addFunction, (nint)add, HookOrder.Earliest);
    }

    internal static IPlayerListNamePrivacySink? Sink => _sink;

    internal static void Bind(IPlayerListNamePrivacySink sink) => _sink = sink;

    internal static void Unbind(IPlayerListNamePrivacySink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int SendPlayerListAdd(void* message, uint recipientId, void* newPlayer)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, uint, void*, int>)_addHook->m_trampoline)(message, recipientId, newPlayer)
            : sink.SendPlayerListAdd(message, recipientId, newPlayer, (nint)_addHook->m_trampoline);
    }
}
