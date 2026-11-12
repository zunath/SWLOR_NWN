using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hooks of the game's <c>Native/CombatRecorderEffectHooks</c>: every damage and heal effect the engine applies. The
/// game enables them the first time a recording starts. Each hook is installed on its own, so a hook that installed stays
/// installed even if the other one fails and a later retry never hooks the same function twice; each keeps the
/// trampoline NWNX gave it when it was installed.
/// </summary>
internal static unsafe class CombatRecorderEffectHooksTrampolines
{
    internal const string ApplyDamageSymbol = "_ZN21CNWSEffectListHandler13OnApplyDamageEP10CNWSObjectP11CGameEffecti";
    internal const string ApplyHealSymbol = "_ZN21CNWSEffectListHandler11OnApplyHealEP10CNWSObjectP11CGameEffecti";

    private static nint _applyDamageFunction;
    private static nint _applyHealFunction;
    private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyDamage;
    private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyHeal;
    private static volatile ICombatRecorderEffectHooksSink? _sink;

    internal static bool IsEnabled => _originalApplyDamage != null && _originalApplyHeal != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_applyDamageFunction == 0)
            _applyDamageFunction = native.GetExport(ApplyDamageSymbol);
        if (_applyHealFunction == 0)
            _applyHealFunction = native.GetExport(ApplyHealSymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        // Earliest: these hooks only observe, and running outermost lets the before/after reads include every other
        // hook's adjustments. The original is reached through the hook's trampoline, not the returned FunctionHook pointer.
        Resolve(native);
        if (_originalApplyDamage == null)
        {
            delegate* unmanaged<void*, void*, void*, int, int> damageHook = &OnApplyDamage;
            _originalApplyDamage = RequestHook(native, _applyDamageFunction, damageHook);
        }

        if (_originalApplyHeal == null)
        {
            delegate* unmanaged<void*, void*, void*, int, int> healHook = &OnApplyHeal;
            _originalApplyHeal = RequestHook(native, _applyHealFunction, healHook);
        }
    }

    private static delegate* unmanaged<void*, void*, void*, int, int> RequestHook(
        INativeFunctions native, nint function, delegate* unmanaged<void*, void*, void*, int, int> hook)
    {
        var functionHook = native.RequestHook(function, (nint)hook, HookOrder.Earliest);
        return (delegate* unmanaged<void*, void*, void*, int, int>)functionHook->m_trampoline;
    }

    internal static ICombatRecorderEffectHooksSink? Sink => _sink;

    internal static void Bind(ICombatRecorderEffectHooksSink sink) => _sink = sink;

    internal static void Unbind(ICombatRecorderEffectHooksSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int OnApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
    {
        var sink = _sink;
        var original = _originalApplyDamage;
        return sink is null
            ? original(effectListHandler, pObject, pEffect, bLoadingGame)
            : sink.ApplyDamage(effectListHandler, pObject, pEffect, bLoadingGame, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int OnApplyHeal(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
    {
        var sink = _sink;
        var original = _originalApplyHeal;
        return sink is null
            ? original(effectListHandler, pObject, pEffect, bLoadingGame)
            : sink.ApplyHeal(effectListHandler, pObject, pEffect, bLoadingGame, (nint)original);
    }
}
