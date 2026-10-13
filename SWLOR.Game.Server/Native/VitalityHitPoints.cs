using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

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

        [ThreadStatic] private static nint _recalculatingStats;
        [ThreadStatic] private static int _calculatedScore;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_maximumHook != null) return;
            var program = NativeLibrary.GetMainProgramHandle();
            delegate* unmanaged<void*, int, short> maximum = &GetMaximum;
            delegate* unmanaged<void*, byte, int, void> setBase = &SetBase;
            delegate* unmanaged<void*, void*, int, int, void> applyEffect = &ApplyEffect;
            delegate* unmanaged<void*, void*, void> removeEffect = &RemoveEffect;
            delegate* unmanaged<void*, ulong, int> removeById = &RemoveEffectById;
            delegate* unmanaged<void*, byte, sbyte> modifier = &CalculateModifier;
            _maximumHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN12CNWSCreature15GetMaxHitPointsEi"), (IntPtr)maximum, HookOrder.Late);
            _baseHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN17CNWSCreatureStats10SetCONBaseEhi"), (IntPtr)setBase, HookOrder.Early);
            // The outer lifecycle includes attribute recalculation after effect handlers return.
            _appliedHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN10CNWSObject11ApplyEffectEP11CGameEffectii"),
                (IntPtr)applyEffect, (int)HookOrder.Earliest - 1);
            _removedHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN10CNWSObject12RemoveEffectEP11CGameEffect"),
                (IntPtr)removeEffect, (int)HookOrder.Earliest - 1);
            _removedByIdHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN10CNWSObject16RemoveEffectByIdEm"), (IntPtr)removeById, HookOrder.Early);
            _modifierHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN17CNWSCreatureStats16CalcStatModifierEh"), (IntPtr)modifier, HookOrder.Late);
        }

        [UnmanagedCallersOnly]
        private static short GetMaximum(void* pointer, int includeToughness)
        {
            var original = (delegate* unmanaged<void*, int, short>)_maximumHook->m_trampoline;
            var maximum = original(pointer, includeToughness);
            try
            {
                var stats = CNWSCreature.FromPointer(pointer).m_pStats;
                if (stats.m_bIsPC == 0) return maximum;
                var recalculating = stats.Pointer == _recalculatingStats;
                var vitality = recalculating ? _calculatedScore : stats.GetCONStat();
                if ((vitality & 1) == 0) return maximum;

                var eligibleLevels = (int)stats.GetLevel(0);
                var modifier = unchecked((sbyte)stats.m_nConstitutionModifier);
                if (modifier < 0)
                {
                    // Native player HP has a one-HP floor on each individual hit die.
                    eligibleLevels = 0;
                    for (byte level = 0; level < stats.GetLevel(0); level++)
                        if (stats.GetLevelStats(level).m_nHitDie + modifier >= 1)
                            eligibleLevels++;
                }
                return (short)Math.Clamp(maximum + CalculateOddScoreBonus(vitality, eligibleLevels), 1, short.MaxValue);
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Could not calculate player Vitality HP");
                return maximum;
            }
        }

        [UnmanagedCallersOnly]
        private static void SetBase(void* pointer, byte score, int recalculateHP)
        {
            var original = (delegate* unmanaged<void*, byte, int, void>)_baseHook->m_trampoline;
            var stats = CNWSCreatureStats.FromPointer(pointer);
            if (stats.m_bIsPC == 0)
            {
                original(pointer, score, recalculateHP);
                return;
            }
            RecalculateEffect(stats, () => original(pointer, score, recalculateHP));
        }

        [UnmanagedCallersOnly]
        private static void ApplyEffect(void* objectPointer, void* effectPointer, int loading, int initial)
        {
            var original = (delegate* unmanaged<void*, void*, int, int, void>)_appliedHook->m_trampoline;
            var stats = GetVitalityEffectStats(objectPointer, effectPointer);
            if (stats == null) original(objectPointer, effectPointer, loading, initial);
            else RecalculateEffect(stats, () => original(objectPointer, effectPointer, loading, initial));
        }

        [UnmanagedCallersOnly]
        private static void RemoveEffect(void* objectPointer, void* effectPointer)
        {
            var original = (delegate* unmanaged<void*, void*, void>)_removedHook->m_trampoline;
            var stats = GetVitalityEffectStats(objectPointer, effectPointer);
            if (stats == null) original(objectPointer, effectPointer);
            else RecalculateEffect(stats, () => original(objectPointer, effectPointer));
        }

        [UnmanagedCallersOnly]
        private static int RemoveEffectById(void* objectPointer, ulong id)
        {
            var original = (delegate* unmanaged<void*, ulong, int>)_removedByIdHook->m_trampoline;
            var creature = CNWSObject.FromPointer(objectPointer).AsNWSCreature();
            if (creature == null || creature.m_pStats.m_bIsPC == 0)
                return original(objectPointer, id);
            foreach (var effect in creature.m_appliedEffects)
            {
                if (effect.m_nID != id || GetVitalityEffectStats(objectPointer, (void*)effect.Pointer) == null)
                    continue;
                var result = 0;
                RecalculateEffect(creature.m_pStats, () => result = original(objectPointer, id));
                return result;
            }
            return original(objectPointer, id);
        }
        private static CNWSCreatureStats GetVitalityEffectStats(void* objectPointer, void* effectPointer)
        {
            if (objectPointer == null || effectPointer == null) return null;
            var effect = CGameEffect.FromPointer(effectPointer);
            if ((effect.m_nType != (ushort)EffectTypeEngine.AbilityIncrease &&
                 effect.m_nType != (ushort)EffectTypeEngine.AbilityDecrease) ||
                effect.GetInteger(0) != (int)AbilityType.Vitality)
                return null;
            var creature = CNWSObject.FromPointer(objectPointer).AsNWSCreature();
            return creature != null && creature.m_pStats.m_bIsPC != 0 ? creature.m_pStats : null;
        }

        private static void RecalculateEffect(CNWSCreatureStats stats, Action apply)
        {
            if (stats.Pointer == _recalculatingStats)
            {
                apply();
                return;
            }
            var previous = (_recalculatingStats, _calculatedScore);
            _calculatedScore = stats.GetCONStat();
            _recalculatingStats = stats.Pointer;
            try { apply(); }
            finally { (_recalculatingStats, _calculatedScore) = previous; }
        }

        [UnmanagedCallersOnly]
        private static sbyte CalculateModifier(void* pointer, byte score)
        {
            var original = (delegate* unmanaged<void*, byte, sbyte>)_modifierHook->m_trampoline;
            var result = original(pointer, score);
            // Both native CON update paths read the previous maximum, recalculate the
            // cached modifier, then apply the maximum's delta to current HP. Switch the
            // fractional contribution at that same boundary without changing the modifier.
            if ((nint)pointer == _recalculatingStats && score == CNWSCreatureStats.FromPointer(pointer).GetCONStat())
                _calculatedScore = score;
            return result;
        }

        public static int CalculateOddScoreBonus(int vitality, int eligibleLevels)
            => (vitality & 1) == 0 ? 0 : Math.Max(0, eligibleLevels) / 2;
    }
}