using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Gives player Vitality its half-level HP contribution at odd scores.</summary>
    public static unsafe class VitalityHitPoints
    {
        private static FunctionHook* _maximumHook;
        private static FunctionHook* _baseHook;
        private static FunctionHook* _appliedHook;
        private static FunctionHook* _removedHook;
        private static FunctionHook* _removedByIdHook;
        private static FunctionHook* _modifierHook;
        private static FunctionHook* _destroyedStatsHook;
        private static IVitalityHitPointsSink _sink;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_maximumHook != null) return;
            _sink ??= new VitalityHitPointsSink();
            var program = NativeLibrary.GetMainProgramHandle();
            // Resolve every symbol before installing any hook; enable the HP bonus last.
            var maximumAddress = NativeLibrary.GetExport(program, "_ZN12CNWSCreature15GetMaxHitPointsEi");
            var baseAddress = NativeLibrary.GetExport(program, "_ZN17CNWSCreatureStats10SetCONBaseEhi");
            var appliedAddress = NativeLibrary.GetExport(program, "_ZN10CNWSObject11ApplyEffectEP11CGameEffectii");
            var removedAddress = NativeLibrary.GetExport(program, "_ZN10CNWSObject12RemoveEffectEP11CGameEffect");
            var removedByIdAddress = NativeLibrary.GetExport(program, "_ZN10CNWSObject16RemoveEffectByIdEm");
            var modifierAddress = NativeLibrary.GetExport(program, "_ZN17CNWSCreatureStats16CalcStatModifierEh");
            var destroyedStatsAddress = NativeLibrary.GetExport(program, "_ZN17CNWSCreatureStatsD1Ev");
            delegate* unmanaged<void*, int, short> maximum = &GetMaximum;
            delegate* unmanaged<void*, byte, int, void> setBase = &SetBase;
            delegate* unmanaged<void*, void*, int, int, void> applyEffect = &ApplyEffect;
            delegate* unmanaged<void*, void*, void> removeEffect = &RemoveEffect;
            delegate* unmanaged<void*, ulong, int> removeById = &RemoveEffectById;
            delegate* unmanaged<void*, byte, sbyte> modifier = &CalculateModifier;
            delegate* unmanaged<void*, void> destroyedStats = &DestroyStats;
            _baseHook = NWNXAPI.RequestFunctionHook(baseAddress, (IntPtr)setBase, HookOrder.Early);
            // The outer lifecycle includes attribute recalculation after effect handlers return.
            _appliedHook = NWNXAPI.RequestFunctionHook(appliedAddress, (IntPtr)applyEffect, (int)HookOrder.Earliest - 1);
            _removedHook = NWNXAPI.RequestFunctionHook(removedAddress, (IntPtr)removeEffect, (int)HookOrder.Earliest - 1);
            _removedByIdHook = NWNXAPI.RequestFunctionHook(removedByIdAddress, (IntPtr)removeById, HookOrder.Early);
            _modifierHook = NWNXAPI.RequestFunctionHook(modifierAddress, (IntPtr)modifier, HookOrder.Late);
            _destroyedStatsHook = NWNXAPI.RequestFunctionHook(destroyedStatsAddress, (IntPtr)destroyedStats, HookOrder.Early);
            _maximumHook = NWNXAPI.RequestFunctionHook(maximumAddress, (IntPtr)maximum, HookOrder.Late);
        }

        [UnmanagedCallersOnly]
        private static short GetMaximum(void* pointer, int includeToughness)
            => _sink.GetMaximum(pointer, includeToughness, (nint)_maximumHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static void SetBase(void* pointer, byte score, int recalculateHP)
            => _sink.SetBase(pointer, score, recalculateHP, (nint)_baseHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static void ApplyEffect(void* objectPointer, void* effectPointer, int loading, int initial)
            => _sink.ApplyEffect(objectPointer, effectPointer, loading, initial, (nint)_appliedHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static void RemoveEffect(void* objectPointer, void* effectPointer)
            => _sink.RemoveEffect(objectPointer, effectPointer, (nint)_removedHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static int RemoveEffectById(void* objectPointer, ulong id)
            => _sink.RemoveEffectById(objectPointer, id, (nint)_removedByIdHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static void DestroyStats(void* pointer)
            => _sink.DestroyStats(pointer, (nint)_destroyedStatsHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static sbyte CalculateModifier(void* pointer, byte score)
            => _sink.CalculateModifier(pointer, score, (nint)_modifierHook->m_trampoline);

        public static short AddBonus(short maximum, int bonus)
            => bonus == 0 ? maximum : (short)Math.Min(maximum + bonus, short.MaxValue);

        public static int CalculateOddScoreBonus(int vitality, int eligibleLevels)
            => (vitality & 1) == 0 ? 0 : Math.Max(0, eligibleLevels) / 2;
    }
}
