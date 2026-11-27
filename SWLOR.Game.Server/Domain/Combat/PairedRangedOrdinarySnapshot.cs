namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Bonuses captured before an ordinary attempt spends its limited charges.</summary>
    public readonly record struct PairedRangedOrdinarySnapshot(
        int DamagePercentAdjustment,
        int AccuracyAdjustment);
}
