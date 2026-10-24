using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.CombatSim.Data
{
    /// <summary>
    /// A weapon-ability profile's target-conditional status (ApplyTargetConditionalStatus): applied after a successful hit
    /// when the target already has the required status or category; a conversion replaces the required status.
    /// </summary>
    public sealed class ConditionalTargetStatus
    {
        public Type Status { get; init; }
        public int DurationSeconds { get; init; }
        public Type RequiredStatus { get; init; }
        public StatusEffectCategory RequiredCategory { get; init; }
        public bool ConvertsRequiredStatus { get; init; }
    }
}
