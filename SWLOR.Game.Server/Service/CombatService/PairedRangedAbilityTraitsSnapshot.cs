using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>One activation's frozen trait payload, consumed only after valid payment.</summary>
    public sealed record PairedRangedAbilityTraitsSnapshot(
        Guid ActivationId,
        bool UsesNextAbilityAccuracy,
        bool SpendsCadence,
        int CadenceSpent,
        int AccuracyAdjustment)
    {
        public IReadOnlyList<long> CadenceChargeIds { get; init; } = Array.Empty<long>();
        public long CadenceAccuracyChargeId { get; init; }
        public int StateGeneration { get; init; }
    }
}
