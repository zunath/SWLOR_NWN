namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>Metadata for a source-owned status that causes ranged hits to add Suppression.</summary>
    public interface IRangedHitSuppressionSource
    {
        uint Source { get; }
        int SuppressionPenaltyPercent { get; }
        int SuppressionDurationSeconds { get; }
    }
}
