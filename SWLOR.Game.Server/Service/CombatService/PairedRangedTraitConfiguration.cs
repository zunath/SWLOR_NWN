namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Stat-derived inputs for paired ranged transient combat state.</summary>
    public sealed record PairedRangedTraitConfiguration
    {
        public int OrdinaryHitRestoreCount { get; init; }
        public int OrdinaryHitStaminaRestore { get; init; }
        public int OrdinaryHitRestoreCooldownSeconds { get; init; }
        public int RepeatedTargetRequiredHits { get; init; }
        public int RepeatedTargetDamagePercent { get; init; }
        public int RepeatedTargetDurationSeconds { get; init; }
        public int DirectDamageNextAbilityStaminaReduction { get; init; }
        public int DirectDamageCostWindowSeconds { get; init; }
        public int DirectDamageCostCooldownSeconds { get; init; }
        public int CadenceMaximum { get; init; }
        public int CadenceDurationSeconds { get; init; }
        public int CadenceChainWindowSeconds { get; init; }
        public int CadenceAccuracyPerCharge { get; init; }
        public int CadenceDamagePerCharge { get; init; }
        public int CadenceBonusDamagePerCharge { get; init; }
        public int CadenceStaminaRestorePerCharge { get; init; }
        public int CadenceStaminaRestoreMaximum { get; init; }
        public int CadenceRestoreCooldownSeconds { get; init; }
        public int EvasionCadenceCooldownSeconds { get; init; }
        public int CadenceNextAbilityAccuracy { get; init; }
        public int CadenceAccuracyDurationSeconds { get; init; }
        public int CadenceAccuracyCooldownSeconds { get; init; }
        public int CadenceExtendedDurationSeconds { get; init; }
    }
}
