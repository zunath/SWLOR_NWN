using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>
/// Stat-driven rewards for ordinary hostile weapon hits across all weapon skills. The logic that used to be static in
/// <c>Service.OrdinaryAttackTraits</c> is moved here unchanged; the per-actor haste state is this instance's own. Calls
/// into the unconverted <c>Stat</c> and <c>StatusEffect</c> services sit behind one-line <c>protected virtual</c>
/// methods whose bodies are the original calls.
/// </summary>
public class OrdinaryAttackTraitsService : IOrdinaryAttackTraitsService
{
    [ReloadState(ReloadStateKind.Carried)]
    private readonly Dictionary<uint, OrdinaryAttackHasteState> _states = new();
    private readonly IStatService _stat;
    private readonly IStatusEffectService _statusEffect;

    public OrdinaryAttackTraitsService(IStatService stat, IStatusEffectService statusEffect)
    {
        _stat = stat;
        _statusEffect = statusEffect;
    }

    public void RecordTargetChange(uint actor, uint target)
    {
        if (!_states.TryGetValue(actor, out var state)) return;
        state.RecordTargetChange(target);
        Sync(actor, state);
    }

    public void RecordHit(uint actor, uint target, long rollId)
    {
        if (!GetIsObjectValid(actor) || GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0 ||
            !GetIsObjectValid(target) || target == actor || !GetIsReactionTypeHostile(target, actor)) return;
        var configuration = GetConfiguration(actor);
        if (configuration.MaximumStacks <= 0) return;
        if (!_states.TryGetValue(actor, out var state)) _states[actor] = state = new OrdinaryAttackHasteState();
        var refresh = state.RecordHit(Clock.UtcNow, configuration, target, rollId);
        Sync(actor, state, refresh);
    }

    public void Reconcile(uint actor)
    {
        if (!_states.TryGetValue(actor, out var state)) return;
        if (!GetIsObjectValid(actor)) { Clear(actor); return; }
        if (GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0) { ClearOnDeath(actor); return; }
        state.Reconcile(Clock.UtcNow, GetConfiguration(actor));
        Sync(actor, state);
    }

    public void Clear(uint actor)
    {
        if (_states.TryGetValue(actor, out var state)) state.Clear();
        RemoveHasteEffect(actor);
    }

    public void ClearOnDeath(uint actor)
    {
        if (_states.TryGetValue(actor, out var state)) state.Reset();
        RemoveHasteEffect(actor);
    }

    public void ClearActor(uint actor)
    {
        Clear(actor);
        _states.Remove(actor);
    }

    /// <summary>A destroyed actor's object ID may be reused; drop its state without touching the object.</summary>
    public void ForgetDestroyed(uint actor) => _states.Remove(actor);

    private OrdinaryAttackHasteConfiguration GetConfiguration(uint actor) => new(
        GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampPerStack),
        GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampMaximumStacks),
        GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampGrantCooldownSeconds),
        GetStatAdjustment(actor, StatType.OrdinaryAttackHasteRampDurationSeconds));

    private void Sync(uint actor, OrdinaryAttackHasteState state, bool refreshTimer = false)
    {
        if (!GetIsObjectValid(actor)) return;
        var remaining = (float)Math.Max(0d, (state.Expires - Clock.UtcNow).TotalSeconds);
        var existing = GetHasteEffect(actor);
        if (state.Stacks <= 0 || remaining <= 0)
        {
            if (existing != null) RemoveHasteEffect(actor);
            return;
        }
        var perStack = GetConfiguration(actor).HastePerStack;
        if (existing != null && existing.Stacks == state.Stacks && existing.HastePerStack == perStack)
        {
            if (refreshTimer)
                RefreshHasteEffectDuration(actor, remaining);
        }
        else
        {
            if (existing != null) RemoveHasteEffect(actor);
            ApplyHasteEffect(actor, state.Stacks, perStack, remaining);
        }
    }

    /// <summary>The unconverted <c>Stat</c> service.</summary>
    protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);

    /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
    protected virtual OrdinaryAttackHasteStatusEffect GetHasteEffect(uint actor) =>
        _statusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor);

    /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
    protected virtual void RemoveHasteEffect(uint actor) =>
        _statusEffect.RemoveStatusEffect<OrdinaryAttackHasteStatusEffect>(actor);

    /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
    protected virtual void RefreshHasteEffectDuration(uint actor, float remaining) =>
        _statusEffect.RefreshStatusEffectDuration(actor, typeof(OrdinaryAttackHasteStatusEffect), actor, remaining);

    /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
    protected virtual void ApplyHasteEffect(uint actor, int stacks, int hastePerStack, float remaining) =>
        _statusEffect.ApplyStatusEffect(actor, actor, new OrdinaryAttackHasteStatusEffect(stacks, hastePerStack), remaining);
}
