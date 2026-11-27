using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The seven hooks of the game's <c>Native/VitalityHitPoints</c>. Every symbol is resolved before any hook is installed,
/// and the HP bonus (<c>GetMaxHitPoints</c>) is installed last and marks the file installed. <c>GetMaxHitPoints</c> and
/// <c>CalcStatModifier</c> run per object per frame. The nested recalculation of the current thread, which the sink keeps
/// across these hooks, is <see cref="VitalityRecalculation.Current"/>.
/// </summary>
internal static unsafe class VitalityHitPointsTrampolines
{
    internal const string MaximumSymbol = "_ZN12CNWSCreature15GetMaxHitPointsEi";
    internal const string BaseSymbol = "_ZN17CNWSCreatureStats10SetCONBaseEhi";
    internal const string AppliedSymbol = "_ZN10CNWSObject11ApplyEffectEP11CGameEffectii";
    internal const string RemovedSymbol = "_ZN10CNWSObject12RemoveEffectEP11CGameEffect";
    internal const string RemovedByIdSymbol = "_ZN10CNWSObject16RemoveEffectByIdEm";
    internal const string ModifierSymbol = "_ZN17CNWSCreatureStats16CalcStatModifierEh";
    internal const string DestroyedStatsSymbol = "_ZN17CNWSCreatureStatsD1Ev";

    private static bool _resolved;
    private static nint _maximumFunction;
    private static nint _baseFunction;
    private static nint _appliedFunction;
    private static nint _removedFunction;
    private static nint _removedByIdFunction;
    private static nint _modifierFunction;
    private static nint _destroyedStatsFunction;
    private static FunctionHook* _maximumHook;
    private static FunctionHook* _baseHook;
    private static FunctionHook* _appliedHook;
    private static FunctionHook* _removedHook;
    private static FunctionHook* _removedByIdHook;
    private static FunctionHook* _modifierHook;
    private static FunctionHook* _destroyedStatsHook;
    private static volatile IVitalityHitPointsSink? _sink;

    internal static bool IsEnabled => _maximumHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_resolved)
            return;

        _maximumFunction = native.GetExport(MaximumSymbol);
        _baseFunction = native.GetExport(BaseSymbol);
        _appliedFunction = native.GetExport(AppliedSymbol);
        _removedFunction = native.GetExport(RemovedSymbol);
        _removedByIdFunction = native.GetExport(RemovedByIdSymbol);
        _modifierFunction = native.GetExport(ModifierSymbol);
        _destroyedStatsFunction = native.GetExport(DestroyedStatsSymbol);
        _resolved = true;
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_maximumHook != null)
            return;

        // Resolve every symbol before installing any hook; enable the HP bonus last.
        Resolve(native);
        delegate* unmanaged<void*, int, short> maximum = &GetMaximum;
        delegate* unmanaged<void*, byte, int, void> setBase = &SetBase;
        delegate* unmanaged<void*, void*, int, int, void> applyEffect = &ApplyEffect;
        delegate* unmanaged<void*, void*, void> removeEffect = &RemoveEffect;
        delegate* unmanaged<void*, ulong, int> removeById = &RemoveEffectById;
        delegate* unmanaged<void*, byte, sbyte> modifier = &CalculateModifier;
        delegate* unmanaged<void*, void> destroyedStats = &DestroyStats;
        _baseHook = native.RequestHook(_baseFunction, (nint)setBase, HookOrder.Early);
        // The outer lifecycle includes attribute recalculation after effect handlers return.
        _appliedHook = native.RequestHook(_appliedFunction, (nint)applyEffect, (int)HookOrder.Earliest - 1);
        _removedHook = native.RequestHook(_removedFunction, (nint)removeEffect, (int)HookOrder.Earliest - 1);
        _removedByIdHook = native.RequestHook(_removedByIdFunction, (nint)removeById, HookOrder.Early);
        _modifierHook = native.RequestHook(_modifierFunction, (nint)modifier, HookOrder.Late);
        _destroyedStatsHook = native.RequestHook(_destroyedStatsFunction, (nint)destroyedStats, HookOrder.Early);
        _maximumHook = native.RequestHook(_maximumFunction, (nint)maximum, HookOrder.Late);
    }

    internal static IVitalityHitPointsSink? Sink => _sink;

    internal static void Bind(IVitalityHitPointsSink sink) => _sink = sink;

    internal static void Unbind(IVitalityHitPointsSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static short GetMaximum(void* pointer, int includeToughness)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, int, short>)_maximumHook->m_trampoline)(pointer, includeToughness)
            : sink.GetMaximum(pointer, includeToughness, (nint)_maximumHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static void SetBase(void* pointer, byte score, int recalculateHP)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, byte, int, void>)_baseHook->m_trampoline)(pointer, score, recalculateHP);
        else
            sink.SetBase(pointer, score, recalculateHP, (nint)_baseHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static void ApplyEffect(void* objectPointer, void* effectPointer, int loading, int initial)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, void*, int, int, void>)_appliedHook->m_trampoline)(objectPointer, effectPointer, loading, initial);
        else
            sink.ApplyEffect(objectPointer, effectPointer, loading, initial, (nint)_appliedHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static void RemoveEffect(void* objectPointer, void* effectPointer)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, void*, void>)_removedHook->m_trampoline)(objectPointer, effectPointer);
        else
            sink.RemoveEffect(objectPointer, effectPointer, (nint)_removedHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static int RemoveEffectById(void* objectPointer, ulong id)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, ulong, int>)_removedByIdHook->m_trampoline)(objectPointer, id)
            : sink.RemoveEffectById(objectPointer, id, (nint)_removedByIdHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static void DestroyStats(void* pointer)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, void>)_destroyedStatsHook->m_trampoline)(pointer);
        else
            sink.DestroyStats(pointer, (nint)_destroyedStatsHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static sbyte CalculateModifier(void* pointer, byte score)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, byte, sbyte>)_modifierHook->m_trampoline)(pointer, score)
            : sink.CalculateModifier(pointer, score, (nint)_modifierHook->m_trampoline);
    }
}
