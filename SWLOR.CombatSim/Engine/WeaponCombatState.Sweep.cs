using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CombatSim.Engine;

public sealed partial class WeaponCombatState
{
    private static readonly HashSet<StatType> SweepSupportedStats = new()
    {
        StatType.AreaAbilityHasteStackMinimumTargets, StatType.AreaAbilityHastePerStack,
        StatType.AreaAbilityHasteStackDurationSeconds, StatType.AreaAbilityHasteStackMaximumPercent,
        StatType.AreaAbilityHasteStacksPerAdditionalTarget, StatType.AreaAbilityUsedEvasionPercentAdjustment,
        StatType.AreaAbilityUsedEvasionDurationSeconds, StatType.AreaHitStaminaRestorePerTarget,
        StatType.AreaHitStaminaRestoreMaximum, StatType.AvoidedAttackAccuracyPercentAdjustment,
        StatType.AvoidedAttackAccuracyDurationSeconds, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment,
        StatType.AvoidedAttackNextSkillAbilityWindowSeconds, StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost,
        StatType.CostlyAbilityHitStaminaRestore
    };
    private readonly Dictionary<int, List<(int Amount, double Until)>> _areaHaste = new();
    public double AreaEvasionUntil { get; private set; }
    public double AvoidedAccuracyUntil { get; private set; }
    private double _avoidDiscountUntil;

    public int AccuracyPercent(double now) => (now < AvoidedAccuracyUntil ? Stat(StatType.AvoidedAttackAccuracyPercentAdjustment) : 0) +
        Stat(StatType.AccuracyPercentAdjustment);
    public int AreaHaste(double now) => _areaHaste.Values.SelectMany(x => x).Where(x => now < x.Until).Sum(x => x.Amount);

    public int AvoidedAttackDiscount(int cost, bool consume, double now)
    {
        var discount = now < _avoidDiscountUntil ? Math.Min(cost, Math.Max(0, -Stat(StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment))) : 0;
        if (consume && discount > 0)
        {
            _avoidDiscountUntil = 0;
            Count("AvoidedAttackStaminaSaved", discount);
        }
        return discount;
    }

    public void AvoidedAttack(double now)
    {
        if (Stat(StatType.AvoidedAttackAccuracyPercentAdjustment) != 0)
            AvoidedAccuracyUntil = now + Stat(StatType.AvoidedAttackAccuracyDurationSeconds);
        if (Stat(StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment) < 0)
            _avoidDiscountUntil = now + Stat(StatType.AvoidedAttackNextSkillAbilityWindowSeconds);
    }

    public void AreaUsed(double now, bool isSweep)
    {
        AreaEvasionUntil = now + Stat(StatType.AreaAbilityUsedEvasionDurationSeconds);
        if (isSweep) Count("SweepCasts");
        Count("AreaCasts");
    }

    public void CostlyAbilityHit(TechniqueActivation activation)
    {
        var minimum = Stat(StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost);
        if (activation.CostlyHitRefundApplied || minimum <= 0 || activation.PaidStamina < minimum) return;
        activation.CostlyHitRefundApplied = true;
        RestoreHitStamina(activation, Stat(StatType.CostlyAbilityHitStaminaRestore), "CostlyHitStaminaRefunded");
    }

    public double RestoreHitStamina(TechniqueActivation activation, int requested, string diagnostic, bool retainMinimumCost = true)
    {
        var refund = retainMinimumCost
            ? Combat.CalculateAbilityHitStaminaRestore(activation.PaidStamina, activation.HitStaminaRefunded, requested)
            : Math.Clamp(requested, 0, Math.Max(0, activation.PaidStamina - activation.HitStaminaRefunded));
        activation.HitStaminaRefunded += refund;
        var restored = Math.Min(_owner.Spec.MaxStm - _owner.Stm, refund);
        _owner.Stm += restored;
        _owner.Stats.StmRefunded += restored;
        if (restored > 0) Count(diagnostic, restored);
        return restored;
    }

    public void CompleteSweep(TechniqueActivation activation, double now)
    {
        if (!activation.IsArea && !activation.IsSweep) return;
        if (activation.IsSweep) Count("SweepTargetsHit", activation.ImpactedTargets);
        Count("AreaTargetsHit", activation.ImpactedTargets);
        var sources = AllSources.ToList();
        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            var threshold = source.GetValueOrDefault(StatType.AreaAbilityHasteStackMinimumTargets);
            if (threshold > 0 && activation.ImpactedTargets >= threshold)
            {
                var stacks = source.GetValueOrDefault(StatType.AreaAbilityHasteStacksPerAdditionalTarget) > 0
                    ? activation.ImpactedTargets - threshold + 1 : 1;
                if (!_areaHaste.TryGetValue(index, out var active)) _areaHaste[index] = active = new();
                active.RemoveAll(x => now >= x.Until);
                var amount = source.GetValueOrDefault(StatType.AreaAbilityHastePerStack);
                var hasteMaximum = source.GetValueOrDefault(StatType.AreaAbilityHasteStackMaximumPercent);
                var duration = source.GetValueOrDefault(StatType.AreaAbilityHasteStackDurationSeconds);
                if (amount > 0 && duration > 0)
                {
                    var added = Math.Min(stacks, (hasteMaximum - active.Sum(x => x.Amount)) / amount);
                    for (var stack = 0; stack < added; stack++)
                        active.Add((amount, now + duration));
                }
            }
            var perTarget = source.GetValueOrDefault(StatType.AreaHitStaminaRestorePerTarget);
            var maximum = source.GetValueOrDefault(StatType.AreaHitStaminaRestoreMaximum);
            if (perTarget > 0 && maximum > 0)
                RestoreHitStamina(activation, Math.Min(maximum, perTarget * activation.ImpactedTargets), "AreaStaminaRefunded");
        }
    }

    private void ClearSweepBenefits()
    {
        _areaHaste.Clear();
        AreaEvasionUntil = AvoidedAccuracyUntil = _avoidDiscountUntil = 0;
    }
}
