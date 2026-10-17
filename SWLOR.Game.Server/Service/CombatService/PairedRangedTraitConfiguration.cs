namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Stat-derived inputs for paired ranged transient combat state.</summary>
    public sealed record PairedRangedTraitConfiguration
    {
        public int OrdinaryDamagePercentAdjustment { get; init; }
        public int CompanionAccuracy { get; init; }
        public int OrdinaryHitRestoreCount { get; init; }
        public int OrdinaryHitStaminaRestore { get; init; }
        public int OrdinaryHitRestoreCooldownSeconds { get; init; }
        public int RepeatedTargetRequiredHits { get; init; }
        public int RepeatedTargetDamagePercent { get; init; }
        public int RepeatedTargetDurationSeconds { get; init; }
        public int DirectDamageNextAbilityStaminaReduction { get; init; }
        public int DirectDamageCostWindowSeconds { get; init; }
        public int DirectDamageCostCooldownSeconds { get; init; }
        public int MomentumMaximum { get; init; }
        public int MomentumDurationSeconds { get; init; }
        public int MomentumAccuracyPerCharge { get; init; }
        public int MomentumStaminaRestorePerCharge { get; init; }
        public int MomentumStaminaRestoreMaximum { get; init; }
        public int MomentumRestoreCooldownSeconds { get; init; }
        public int EvasionMomentumCooldownSeconds { get; init; }
        public int MovementNextAbilityAccuracy { get; init; }
        public int MovementAccuracyDurationSeconds { get; init; }
        public int MovementAccuracyCooldownSeconds { get; init; }
        public int MomentumExtendedDurationSeconds { get; init; }
    }
}
