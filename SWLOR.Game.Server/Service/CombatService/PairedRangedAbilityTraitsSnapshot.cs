using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>One activation's frozen trait payload, consumed only after valid payment.</summary>
    public sealed record PairedRangedAbilityTraitsSnapshot(
        Guid ActivationId,
        bool UsesMovementAccuracy,
        bool SpendsMomentum,
        int MomentumSpent,
        int AccuracyAdjustment)
    {
        public IReadOnlyList<long> MomentumChargeIds { get; init; } = Array.Empty<long>();
        public long MovementAccuracyChargeId { get; init; }
        public int StateGeneration { get; init; }
    }
}
