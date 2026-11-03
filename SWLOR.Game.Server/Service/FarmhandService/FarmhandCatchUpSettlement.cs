namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>What to do with a pending catch-up found at the next catch-up or server start (<see cref="FarmhandCatchUpRules.Settle"/>).</summary>
    public enum FarmhandCatchUpSettlement
    {
        /// <summary>The farmhand is gone or is another assignment's: the catch-up belongs to nothing and is deleted without being applied.</summary>
        Discard = 1,

        /// <summary>The farmhand already holds the catch-up. It is written after every other record, so they hold it too: only the catch-up is deleted.</summary>
        Finished = 2,

        /// <summary>The farmhand does not hold the catch-up yet: every record in it is written again, then it is deleted.</summary>
        Apply = 3
    }
}
