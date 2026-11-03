using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CombatSim.Engine;

/// <summary>Uses the server's state machines; no independently transcribed proc/Cadence rules.</summary>
public sealed class WeaponTraitState
{
    public PairedRangedTraitState Paired { get; } = new();
    public OrdinaryAttackHasteState Haste { get; } = new();
    public long RollId { get; set; }

    /// <summary>Mirrors the server: these stats apply only with two weapons (an off-hand attack).</summary>
    public static int DualWieldStat(Combatant actor, StatType stat) =>
        actor.Spec.WeaponLoadout?.HasOffhand == true ? actor.GetStat(stat) : 0;

    public static PairedRangedTraitConfiguration Configuration(Combatant actor) => new()
    {
        OrdinaryHitRestoreCount = DualWieldStat(actor, StatType.PairedRangedOrdinaryHitRestoreCount),
        OrdinaryHitStaminaRestore = DualWieldStat(actor, StatType.PairedRangedOrdinaryHitStaminaRestore),
        OrdinaryHitRestoreCooldownSeconds = actor.GetStat(StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds),
        RepeatedTargetRequiredHits = actor.GetStat(StatType.PairedRangedRepeatedTargetRequiredHits),
        RepeatedTargetDamagePercent = actor.GetStat(StatType.PairedRangedRepeatedTargetDamagePercent),
        RepeatedTargetDurationSeconds = actor.GetStat(StatType.PairedRangedRepeatedTargetDurationSeconds),
        DirectDamageNextAbilityStaminaReduction = actor.GetStat(StatType.PairedRangedDirectDamageNextAbilityStaminaReduction),
        DirectDamageCostWindowSeconds = actor.GetStat(StatType.PairedRangedDirectDamageCostWindowSeconds),
        DirectDamageCostCooldownSeconds = actor.GetStat(StatType.PairedRangedDirectDamageCostCooldownSeconds),
        CadenceMaximum = actor.GetStat(StatType.CadenceMaximum),
        CadenceDurationSeconds = actor.GetStat(StatType.CadenceDurationSeconds),
        CadenceChainWindowSeconds = actor.GetStat(StatType.CadenceChainWindowSeconds),
        CadenceAccuracyPerCharge = actor.GetStat(StatType.CadenceAccuracyPerCharge),
        CadenceDamagePerCharge = actor.GetStat(StatType.CadenceDamagePerCharge),
        CadenceBonusDamagePerCharge = actor.GetStat(StatType.CadenceBonusDamagePerCharge),
        CadenceStaminaRestorePerCharge = actor.GetStat(StatType.CadenceStaminaRestorePerCharge),
        CadenceStaminaRestoreMaximum = actor.GetStat(StatType.CadenceStaminaRestoreMaximum),
        CadenceRestoreCooldownSeconds = actor.GetStat(StatType.CadenceRestoreCooldownSeconds),
        EvasionCadenceCooldownSeconds = actor.GetStat(StatType.EvasionCadenceCooldownSeconds),
        CadenceNextAbilityAccuracy = actor.GetStat(StatType.CadenceNextAbilityAccuracy),
        CadenceAccuracyDurationSeconds = actor.GetStat(StatType.CadenceAccuracyDurationSeconds),
        CadenceAccuracyCooldownSeconds = actor.GetStat(StatType.CadenceAccuracyCooldownSeconds),
        CadenceExtendedDurationSeconds = actor.GetStat(StatType.CadenceExtendedDurationSeconds)
    };

    public static OrdinaryAttackHasteConfiguration HasteConfiguration(Combatant actor) => new(
        actor.GetStat(StatType.OrdinaryAttackHasteRampPerStack),
        actor.GetStat(StatType.OrdinaryAttackHasteRampMaximumStacks),
        actor.GetStat(StatType.OrdinaryAttackHasteRampGrantCooldownSeconds),
        actor.GetStat(StatType.OrdinaryAttackHasteRampDurationSeconds));
}
