namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>One stat an enzyme rolled: the incubation stat, and the amount in tenths of a percent (11 is 1.1%).</summary>
    public readonly record struct EnzymeStatRoll(IncubationStatType Stat, int Amount);
}
