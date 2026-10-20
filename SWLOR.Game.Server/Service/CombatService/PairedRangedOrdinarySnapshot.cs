namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Bonuses captured before an ordinary attempt spends its limited charges.</summary>
    public readonly record struct PairedRangedOrdinarySnapshot(
        int DamagePercentAdjustment,
        int AccuracyAdjustment);
}
