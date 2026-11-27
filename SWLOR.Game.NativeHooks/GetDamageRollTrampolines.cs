using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hook of the game's <c>Native/GetDamageRoll</c>: <c>CNWSCreatureStats::GetDamageRoll</c>. The sink replaces the
/// engine function and never calls it; only a hook with no sink bound runs it.
/// </summary>
internal static unsafe class GetDamageRollTrampolines
{
    internal const string GetDamageRollSymbol = "_ZN17CNWSCreatureStats13GetDamageRollEP10CNWSObjectiiiii";

    /// <summary>The order the game always hooked the damage roll at.</summary>
    internal const int Order = -1000000;

    private static nint _function;
    private static FunctionHook* _hook;
    private static volatile IGetDamageRollSink? _sink;

    internal static bool IsEnabled => _hook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_function == 0)
            _function = native.GetExport(GetDamageRollSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_hook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, int, int, int, int, int, int> hook = &OnGetDamageRoll;
        _hook = native.RequestHook(_function, (nint)hook, Order);
    }

    internal static IGetDamageRollSink? Sink => _sink;

    internal static void Bind(IGetDamageRollSink sink) => _sink = sink;

    internal static void Unbind(IGetDamageRollSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int OnGetDamageRoll(
        void* thisPtr,
        void* pTarget,
        int bOffHand,
        int bCritical,
        int bSneakAttack,
        int bDeathAttack,
        int bForceMax)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, int, int, int, int, int, int>)_hook->m_trampoline)(
                thisPtr, pTarget, bOffHand, bCritical, bSneakAttack, bDeathAttack, bForceMax)
            : sink.GetDamageRoll(thisPtr, pTarget, bOffHand, bCritical, bSneakAttack, bDeathAttack, bForceMax);
    }
}
