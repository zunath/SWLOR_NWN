namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The stamina a hostile ability activation paid, kept for 35 seconds so its hits can refund part of it and the
    /// costly-ability riders can read it (<see cref="ICombatResourcesService.TryGetAbilityStaminaCostState"/>).
    /// </summary>
    public sealed class CombatAbilityStaminaCostState
    {
        public int Cost { get; init; }
        public DateTime SpentAt { get; init; }
        public int NonCriticalRangedAbilityStaminaCost { get; set; }
        public bool StaminaRestoreApplied { get; set; }
        public int HitStaminaRefunded { get; set; }
        public int DeferredImpactCount { get; set; }
    }
}
