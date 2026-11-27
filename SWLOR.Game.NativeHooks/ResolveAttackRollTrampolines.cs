using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hook of the game's <c>Native/ResolveAttackRoll</c>: <c>CNWSCreature::ResolveAttackRoll</c>. The sink replaces the
/// engine function and never calls it; only a hook with no sink bound runs it.
/// </summary>
internal static unsafe class ResolveAttackRollTrampolines
{
    internal const string ResolveAttackRollSymbol = "_ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject";

    /// <summary>The order the game always hooked the attack roll at.</summary>
    internal const int Order = -1000000;

    private static nint _function;
    private static FunctionHook* _hook;
    private static volatile IResolveAttackRollSink? _sink;

    internal static bool IsEnabled => _hook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_function == 0)
            _function = native.GetExport(ResolveAttackRollSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_hook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void> hook = &OnResolveAttackRoll;
        _hook = native.RequestHook(_function, (nint)hook, Order);
    }

    internal static IResolveAttackRollSink? Sink => _sink;

    internal static void Bind(IResolveAttackRollSink sink) => _sink = sink;

    internal static void Unbind(IResolveAttackRollSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static void OnResolveAttackRoll(void* thisPtr, void* pTarget)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, void*, void>)_hook->m_trampoline)(thisPtr, pTarget);
        else
            sink.ResolveAttackRoll(thisPtr, pTarget);
    }
}
