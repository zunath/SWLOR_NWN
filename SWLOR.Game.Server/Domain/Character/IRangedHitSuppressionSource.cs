namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>Metadata for a source-owned status that causes ranged hits to add Suppression.</summary>
    public interface IRangedHitSuppressionSource
    {
        uint Source { get; }
        int SuppressionPenaltyPercent { get; }
        int SuppressionEvasionPenaltyAdjustment { get; }
        int SuppressionDurationSeconds { get; }
    }
}
