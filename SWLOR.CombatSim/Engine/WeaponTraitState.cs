using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using System.Numerics;

namespace SWLOR.CombatSim.Engine;

/// <summary>Uses the server's state machines; no independently transcribed proc/Flow rules.</summary>
public sealed class WeaponTraitState
{
    public PairedRangedTraitState Paired { get; } = new();
    public OrdinaryAttackHasteState Haste { get; } = new();
    public long RollId { get; set; }
    public double MovementReadyAt { get; set; }
    public WeaponMovementWindow Movement { get; } = new();
    public Vector3 Position { get; set; }

    public static PairedRangedTraitConfiguration Configuration(Combatant actor) => new()
    {
        OrdinaryDamagePercentAdjustment = actor.GetStat(StatType.PairedRangedOrdinaryDamagePercentAdjustment),
        OrdinaryHitRestoreCount = actor.GetStat(StatType.PairedRangedOrdinaryHitRestoreCount),
        OrdinaryHitStaminaRestore = actor.GetStat(StatType.PairedRangedOrdinaryHitStaminaRestore),
        OrdinaryHitRestoreCooldownSeconds = actor.GetStat(StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds),
        RepeatedTargetRequiredHits = actor.GetStat(StatType.PairedRangedRepeatedTargetRequiredHits),
        RepeatedTargetDamagePercent = actor.GetStat(StatType.PairedRangedRepeatedTargetDamagePercent),
        RepeatedTargetDurationSeconds = actor.GetStat(StatType.PairedRangedRepeatedTargetDurationSeconds),
        DirectDamageNextAbilityStaminaReduction = actor.GetStat(StatType.PairedRangedDirectDamageNextAbilityStaminaReduction),
        DirectDamageCostWindowSeconds = actor.GetStat(StatType.PairedRangedDirectDamageCostWindowSeconds),
        DirectDamageCostCooldownSeconds = actor.GetStat(StatType.PairedRangedDirectDamageCostCooldownSeconds),
        MomentumMaximum = actor.GetStat(StatType.PairedRangedMomentumMaximum),
        MomentumDurationSeconds = actor.GetStat(StatType.PairedRangedMomentumDurationSeconds),
        MomentumAccuracyPerCharge = actor.GetStat(StatType.PairedRangedMomentumAccuracyPerCharge),
        MomentumDamagePerCharge = actor.GetStat(StatType.MovementHitWeaponAbilityDamagePerCharge),
        MomentumStaminaRestorePerCharge = actor.GetStat(StatType.PairedRangedMomentumStaminaRestorePerCharge),
        MomentumStaminaRestoreMaximum = actor.GetStat(StatType.PairedRangedMomentumStaminaRestoreMaximum),
        MomentumRestoreCooldownSeconds = actor.GetStat(StatType.PairedRangedMomentumRestoreCooldownSeconds),
        EvasionMomentumCooldownSeconds = actor.GetStat(StatType.PairedRangedEvasionMomentumCooldownSeconds),
        MovementNextAbilityAccuracy = actor.GetStat(StatType.PairedRangedMovementNextAbilityAccuracy),
        MovementAccuracyDurationSeconds = actor.GetStat(StatType.PairedRangedMovementAccuracyDurationSeconds),
        MovementAccuracyCooldownSeconds = actor.GetStat(StatType.PairedRangedMovementAccuracyCooldownSeconds),
        MomentumExtendedDurationSeconds = actor.GetStat(StatType.PairedRangedMomentumExtendedDurationSeconds)
    };

    public static OrdinaryAttackHasteConfiguration HasteConfiguration(Combatant actor) => new(
        actor.GetStat(StatType.OrdinaryAttackHasteRampPerStack),
        actor.GetStat(StatType.OrdinaryAttackHasteRampMaximumStacks),
        actor.GetStat(StatType.OrdinaryAttackHasteRampGrantCooldownSeconds),
        actor.GetStat(StatType.OrdinaryAttackHasteRampDurationSeconds));
}
