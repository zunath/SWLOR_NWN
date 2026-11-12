using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Gives player Vitality its half-level HP contribution at odd scores. Each player's cached Vitality state, the
/// creature-by-stats index and the stats being destroyed live here; the nested recalculation of the current thread is
/// thread-static and lives beside the trampolines (<see cref="VitalityRecalculation.Current"/>), because the engine thread
/// outlives every game build.
/// </summary>
public sealed unsafe class VitalityHitPointsSink : IVitalityHitPointsSink
{
    private readonly Dictionary<nint, VitalityPlayerState> _playerStates = new();
    private readonly Dictionary<nint, nint> _creaturePointersByStats = new();
    private readonly HashSet<nint> _destroyingStats = new();

    public short GetMaximum(void* pointer, int includeToughness, nint original)
    {
        var maximum = ((delegate* unmanaged<void*, int, short>)original)(pointer, includeToughness);
        try
        {
            var state = _playerStates.TryGetValue((nint)pointer, out var cached)
                ? cached : GetPlayerState(CNWSCreature.FromPointer(pointer));
            if (state == null || _destroyingStats.Contains(state.Stats.Pointer) ||
                state.Stats.m_bIsPC == 0 || state.Creature.GetIsPossessedFamiliar() != 0)
                return maximum;
            var stats = state.Stats;
            var recalculation = VitalityRecalculation.Current;
            var vitality = recalculation != null && recalculation.TryGetScore(stats.Pointer, out var score)
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
            var bonus = VitalityHitPoints.CalculateOddScoreBonus(vitality, eligibleLevels);
            return VitalityHitPoints.AddBonus(maximum, bonus);
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Could not calculate player Vitality HP");
            return maximum;
        }
    }

    public void SetBase(void* pointer, byte score, int recalculateHP, nint original)
    {
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
        try { ((delegate* unmanaged<void*, byte, int, void>)original)(pointer, score, recalculateHP); }
        finally { EndRecalculation(scope); }
    }

    public void ApplyEffect(void* objectPointer, void* effectPointer, int loading, int initial, nint original)
    {
        nint scope = 0;
        try { scope = BeginRecalculation(GetVitalityEffectStats(objectPointer, effectPointer)); }
        catch (Exception exception)
        {
            ForgetCreature((nint)objectPointer);
            Log.WriteError(exception, "Could not track applied Vitality HP recalculation");
        }
        try { ((delegate* unmanaged<void*, void*, int, int, void>)original)(objectPointer, effectPointer, loading, initial); }
        finally { EndRecalculation(scope); }
    }

    public void RemoveEffect(void* objectPointer, void* effectPointer, nint original)
    {
        nint scope = 0;
        try { scope = BeginRecalculation(GetVitalityEffectStats(objectPointer, effectPointer)); }
        catch (Exception exception)
        {
            ForgetCreature((nint)objectPointer);
            Log.WriteError(exception, "Could not track removed Vitality HP recalculation");
        }
        try { ((delegate* unmanaged<void*, void*, void>)original)(objectPointer, effectPointer); }
        finally { EndRecalculation(scope); }
    }

    public int RemoveEffectById(void* objectPointer, ulong id, nint original)
    {
        nint scope = 0;
        try
        {
            var state = _playerStates.TryGetValue((nint)objectPointer, out var cached)
                ? cached : GetPlayerState(CNWSObject.FromPointer(objectPointer).AsNWSCreature());
            if (state != null && state.EffectIds.Contains(id))
                scope = BeginRecalculation(state.Stats);
        }
        catch (Exception exception)
        {
            ForgetCreature((nint)objectPointer);
            Log.WriteError(exception, "Could not track removed Vitality effect ID");
        }
        try { return ((delegate* unmanaged<void*, ulong, int>)original)(objectPointer, id); }
        finally { EndRecalculation(scope); }
    }
    private CNWSCreatureStats GetVitalityEffectStats(void* objectPointer, void* effectPointer)
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

    private nint BeginRecalculation(CNWSCreatureStats stats)
    {
        if (stats == null || stats.m_bIsPC == 0) return 0;
        var recalculation = VitalityRecalculation.ForCurrentThread();
        if (recalculation.TryGetScore(stats.Pointer, out _)) return 0;
        GetPlayerState(stats.m_pBaseCreature);
        return recalculation.Begin(stats.Pointer, stats.GetCONStat()) ? stats.Pointer : 0;
    }

    private void EndRecalculation(nint scope)
    {
        if (scope == 0) return;
        var recalculation = VitalityRecalculation.Current;
        try
        {
            if (recalculation.TryGetScore(scope, out _) &&
                _creaturePointersByStats.TryGetValue(scope, out var creature) &&
                _playerStates.TryGetValue(creature, out var state))
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
        finally { recalculation.End(scope); }
    }

    private VitalityPlayerState GetPlayerState(CNWSCreature creature)
    {
        if (creature == null) return null;
        if (_playerStates.TryGetValue(creature.Pointer, out var state)) return state;
        var stats = creature.m_pStats;
        if (stats.m_bIsPC == 0 || _destroyingStats.Contains(stats.Pointer)) return null;
        state = new VitalityPlayerState(creature, stats);
        _playerStates.Add(creature.Pointer, state);
        _creaturePointersByStats.Add(stats.Pointer, creature.Pointer);
        return state;
    }

    private void ForgetCreature(nint creature)
    {
        if (_playerStates.Remove(creature, out var state)) _creaturePointersByStats.Remove(state.Stats.Pointer);
    }

    private void ForgetStats(nint stats)
    {
        if (_creaturePointersByStats.Remove(stats, out var creature)) _playerStates.Remove(creature);
    }

    public void DestroyStats(void* pointer, nint original)
    {
        try
        {
            _destroyingStats.Add((nint)pointer);
            ForgetStats((nint)pointer);
            VitalityRecalculation.Current?.End((nint)pointer);
        }
        catch (Exception exception) { Log.WriteError(exception, "Could not discard destroyed player Vitality state"); }
        try { ((delegate* unmanaged<void*, void>)original)(pointer); }
        finally
        {
            try
            {
                ForgetStats((nint)pointer);
                _destroyingStats.Remove((nint)pointer);
            }
            catch (Exception exception) { Log.WriteError(exception, "Could not finish discarding destroyed player Vitality state"); }
        }
    }

    public sbyte CalculateModifier(void* pointer, byte score, nint original)
    {
        var result = ((delegate* unmanaged<void*, byte, sbyte>)original)(pointer, score);
        // Both native CON update paths read the previous maximum, recalculate the
        // cached modifier, then apply the maximum's delta to current HP. Switch the
        // fractional contribution at that same boundary without changing the modifier.
        try
        {
            var recalculation = VitalityRecalculation.Current;
            if (recalculation != null && recalculation.TryGetScore((nint)pointer, out _) &&
                score == CNWSCreatureStats.FromPointer(pointer).GetCONStat())
                recalculation.Update((nint)pointer, score);
        }
        catch (Exception exception) { Log.WriteError(exception, "Could not track the recalculated Vitality score"); }
        return result;
    }
}
