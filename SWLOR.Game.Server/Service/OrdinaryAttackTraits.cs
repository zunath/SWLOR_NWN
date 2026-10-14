using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service;

/// <summary>Stat-driven rewards for ordinary hostile weapon hits across all weapon skills.</summary>
public static class OrdinaryAttackTraits
{
    private static readonly Dictionary<uint, OrdinaryAttackHasteState> States = new();

    public static void RecordTargetChange(uint actor, uint target)
    {
        if (!States.TryGetValue(actor, out var state)) return;
        state.RecordTargetChange(target);
        Sync(actor, state);
    }

    public static void RecordHit(uint actor, uint target, long rollId)
    {
        if (!GetIsObjectValid(actor) || GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0 ||
            !GetIsObjectValid(target) || target == actor || !GetIsReactionTypeHostile(target, actor)) return;
        var configuration = GetConfiguration(actor);
        if (configuration.MaximumStacks <= 0) return;
        if (!States.TryGetValue(actor, out var state)) States[actor] = state = new OrdinaryAttackHasteState();
        var refresh = state.RecordHit(DateTime.UtcNow, configuration, target, rollId);
        Sync(actor, state, refresh);
    }

    public static void Reconcile(uint actor)
    {
        if (!States.TryGetValue(actor, out var state)) return;
        if (!GetIsObjectValid(actor) || GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0) { Clear(actor); return; }
        state.Reconcile(DateTime.UtcNow, GetConfiguration(actor));
        Sync(actor, state);
    }

    public static void Clear(uint actor)
    {
        if (States.TryGetValue(actor, out var state)) state.Clear();
        StatusEffect.RemoveStatusEffect<OrdinaryAttackHasteStatusEffect>(actor);
    }

    public static void ClearActor(uint actor)
    {
        Clear(actor);
        States.Remove(actor);
    }

    private static OrdinaryAttackHasteConfiguration GetConfiguration(uint actor) => new(
        Stat.GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampPerStack),
        Stat.GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampMaximumStacks),
        Stat.GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampGrantCooldownSeconds),
        Stat.GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampDurationSeconds));

    private static void Sync(uint actor, OrdinaryAttackHasteState state, bool refreshTimer = false)
    {
        if (!GetIsObjectValid(actor)) return;
        var remaining = (float)Math.Max(0d, (state.Expires - DateTime.UtcNow).TotalSeconds);
        var existing = StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor);
        if (state.Stacks <= 0 || remaining <= 0)
        {
            if (existing != null) StatusEffect.RemoveStatusEffect<OrdinaryAttackHasteStatusEffect>(actor);
            return;
        }
        var perStack = GetConfiguration(actor).HastePerStack;
        if (existing != null && existing.Stacks == state.Stacks && existing.HastePerStack == perStack)
        {
            if (refreshTimer)
                StatusEffect.RefreshStatusEffectDuration(actor, typeof(OrdinaryAttackHasteStatusEffect), actor, remaining);
        }
        else
        {
            if (existing != null) StatusEffect.RemoveStatusEffect<OrdinaryAttackHasteStatusEffect>(actor);
            StatusEffect.ApplyStatusEffect(actor, actor, new OrdinaryAttackHasteStatusEffect(state.Stacks, perStack), remaining);
        }
    }
}
