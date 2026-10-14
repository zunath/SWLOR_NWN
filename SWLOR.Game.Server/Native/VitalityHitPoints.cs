using System.Collections.Generic;
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
        private static FunctionHook* _destroyedStatsHook;
        private static readonly Dictionary<nint, VitalityPlayerState> PlayerStates = new();
        private static readonly Dictionary<nint, nint> CreaturePointersByStats = new();
        private static readonly HashSet<nint> DestroyingStats = new();

        [ThreadStatic] private static VitalityRecalculation _recalculation;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_maximumHook != null) return;
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
        {
            var original = (delegate* unmanaged<void*, int, short>)_maximumHook->m_trampoline;
            var maximum = original(pointer, includeToughness);
            try
            {
                var state = PlayerStates.TryGetValue((nint)pointer, out var cached)
                    ? cached : GetPlayerState(CNWSCreature.FromPointer(pointer));
                if (state == null || DestroyingStats.Contains(state.Stats.Pointer) ||
                    state.Stats.m_bIsPC == 0 || state.Creature.GetIsPossessedFamiliar() != 0)
                    return maximum;
                var stats = state.Stats;
                var vitality = _recalculation != null && _recalculation.TryGetScore(stats.Pointer, out var score)
                    ? score : state.Score;
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
                var bonus = CalculateOddScoreBonus(vitality, eligibleLevels);
                return AddBonus(maximum, bonus);
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
            nint scope = 0;
            try
            {
                scope = BeginRecalculation(CNWSCreatureStats.FromPointer(pointer));
            }
            catch (Exception exception)
            {
                ForgetStats((nint)pointer);
                Log.WriteError(exception, "Could not track base Vitality HP recalculation");
            }
            try { original(pointer, score, recalculateHP); }
            finally { EndRecalculation(scope); }
        }

        [UnmanagedCallersOnly]
        private static void ApplyEffect(void* objectPointer, void* effectPointer, int loading, int initial)
        {
            var original = (delegate* unmanaged<void*, void*, int, int, void>)_appliedHook->m_trampoline;
            nint scope = 0;
            try { scope = BeginRecalculation(GetVitalityEffectStats(objectPointer, effectPointer)); }
            catch (Exception exception)
            {
                ForgetCreature((nint)objectPointer);
                Log.WriteError(exception, "Could not track applied Vitality HP recalculation");
            }
            try { original(objectPointer, effectPointer, loading, initial); }
            finally { EndRecalculation(scope); }
        }

        [UnmanagedCallersOnly]
        private static void RemoveEffect(void* objectPointer, void* effectPointer)
        {
            var original = (delegate* unmanaged<void*, void*, void>)_removedHook->m_trampoline;
            nint scope = 0;
            try { scope = BeginRecalculation(GetVitalityEffectStats(objectPointer, effectPointer)); }
            catch (Exception exception)
            {
                ForgetCreature((nint)objectPointer);
                Log.WriteError(exception, "Could not track removed Vitality HP recalculation");
            }
            try { original(objectPointer, effectPointer); }
            finally { EndRecalculation(scope); }
        }

        [UnmanagedCallersOnly]
        private static int RemoveEffectById(void* objectPointer, ulong id)
        {
            var original = (delegate* unmanaged<void*, ulong, int>)_removedByIdHook->m_trampoline;
            nint scope = 0;
            try
            {
                var state = PlayerStates.TryGetValue((nint)objectPointer, out var cached)
                    ? cached : GetPlayerState(CNWSObject.FromPointer(objectPointer).AsNWSCreature());
                if (state != null && state.EffectIds.Contains(id))
                    scope = BeginRecalculation(state.Stats);
            }
            catch (Exception exception)
            {
                ForgetCreature((nint)objectPointer);
                Log.WriteError(exception, "Could not track removed Vitality effect ID");
            }
            try { return original(objectPointer, id); }
            finally { EndRecalculation(scope); }
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

        private static nint BeginRecalculation(CNWSCreatureStats stats)
        {
            if (stats == null || stats.m_bIsPC == 0) return 0;
            _recalculation ??= new VitalityRecalculation();
            if (_recalculation.TryGetScore(stats.Pointer, out _)) return 0;
            GetPlayerState(stats.m_pBaseCreature);
            return _recalculation.Begin(stats.Pointer, stats.GetCONStat()) ? stats.Pointer : 0;
        }

        private static void EndRecalculation(nint scope)
        {
            if (scope == 0) return;
            try
            {
                if (_recalculation.TryGetScore(scope, out _) &&
                    CreaturePointersByStats.TryGetValue(scope, out var creature) &&
                    PlayerStates.TryGetValue(creature, out var state))
                {
                    state.Score = state.Stats.GetCONStat();
                    state.RefreshEffects();
                }
            }
            catch (Exception exception)
            {
                ForgetStats(scope);
                Log.WriteError(exception, "Could not finish Vitality HP recalculation");
            }
            finally { _recalculation.End(scope); }
        }

        private static VitalityPlayerState GetPlayerState(CNWSCreature creature)
        {
            if (creature == null) return null;
            if (PlayerStates.TryGetValue(creature.Pointer, out var state)) return state;
            var stats = creature.m_pStats;
            if (stats.m_bIsPC == 0 || DestroyingStats.Contains(stats.Pointer)) return null;
            state = new VitalityPlayerState(creature, stats);
            PlayerStates.Add(creature.Pointer, state);
            CreaturePointersByStats.Add(stats.Pointer, creature.Pointer);
            return state;
        }

        private static void ForgetCreature(nint creature)
        {
            if (PlayerStates.Remove(creature, out var state)) CreaturePointersByStats.Remove(state.Stats.Pointer);
        }

        private static void ForgetStats(nint stats)
        {
            if (CreaturePointersByStats.Remove(stats, out var creature)) PlayerStates.Remove(creature);
        }

        [UnmanagedCallersOnly]
        private static void DestroyStats(void* pointer)
        {
            var original = (delegate* unmanaged<void*, void>)_destroyedStatsHook->m_trampoline;
            try
            {
                DestroyingStats.Add((nint)pointer);
                ForgetStats((nint)pointer);
                _recalculation?.End((nint)pointer);
            }
            catch (Exception exception) { Log.WriteError(exception, "Could not discard destroyed player Vitality state"); }
            try { original(pointer); }
            finally
            {
                try
                {
                    ForgetStats((nint)pointer);
                    DestroyingStats.Remove((nint)pointer);
                }
                catch (Exception exception) { Log.WriteError(exception, "Could not finish discarding destroyed player Vitality state"); }
            }
        }

        [UnmanagedCallersOnly]
        private static sbyte CalculateModifier(void* pointer, byte score)
        {
            var original = (delegate* unmanaged<void*, byte, sbyte>)_modifierHook->m_trampoline;
            var result = original(pointer, score);
            // Both native CON update paths read the previous maximum, recalculate the
            // cached modifier, then apply the maximum's delta to current HP. Switch the
            // fractional contribution at that same boundary without changing the modifier.
            try
            {
                if (_recalculation != null && _recalculation.TryGetScore((nint)pointer, out _) &&
                    score == CNWSCreatureStats.FromPointer(pointer).GetCONStat())
                    _recalculation.Update((nint)pointer, score);
            }
            catch (Exception exception) { Log.WriteError(exception, "Could not track the recalculated Vitality score"); }
            return result;
        }

        public static short AddBonus(short maximum, int bonus)
            => bonus == 0 ? maximum : (short)Math.Min(maximum + bonus, short.MaxValue);

        public static int CalculateOddScoreBonus(int vitality, int eligibleLevels)
            => (vitality & 1) == 0 ? 0 : Math.Max(0, eligibleLevels) / 2;
    }
}
