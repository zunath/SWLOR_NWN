using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hook of the game's <c>Native/OnAIActionAttackObject</c>: <c>CNWSCreature::AIActionAttackObject</c>. The sink
/// replaces the engine function and never calls it; only a hook with no sink bound runs it.
/// </summary>
internal static unsafe class OnAIActionAttackObjectTrampolines
{
    internal const string AttackObjectSymbol = "_ZN12CNWSCreature20AIActionAttackObjectEP20CNWSObjectActionNode";

    /// <summary>The order the game always hooked the attack action at.</summary>
    internal const int Order = -1000000;

    private static nint _function;
    private static FunctionHook* _hook;
    private static volatile IOnAIActionAttackObjectSink? _sink;

    internal static bool IsEnabled => _hook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_function == 0)
            _function = native.GetExport(AttackObjectSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_hook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, int> hook = &HandleOnAIActionAttackObject;
        _hook = native.RequestHook(_function, (nint)hook, Order);
    }

    internal static IOnAIActionAttackObjectSink? Sink => _sink;

    internal static void Bind(IOnAIActionAttackObjectSink sink) => _sink = sink;

    internal static void Unbind(IOnAIActionAttackObjectSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int HandleOnAIActionAttackObject(void* creature, void* node)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, int>)_hook->m_trampoline)(creature, node)
            : sink.AttackObject(creature, node);
    }
}
