namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>Progress of a Quick Harvest: one unit at a time, each taking a harvest delay.</summary>
    /// <param name="UnitsDone">The units taken so far.</param>
    /// <param name="UnitsTotal">The units it set out to take.</param>
    /// <param name="Xp">The XP paid for the last unit.</param>
    /// <param name="Finished">True when the Quick Harvest is over: the last unit was taken or it was cut short.</param>
    /// <param name="Message">The words for its end, when it ended.</param>
    public sealed record QuickHarvestReport(int UnitsDone, int UnitsTotal, int Xp, bool Finished, string Message);
}
