namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// Where a city bounty stands. Values are saved with the bounty and never change.
    /// </summary>
    public enum BountyState
    {
        Invalid = 0,

        /// <summary>Posted and open to claim until it expires or an official cancels it.</summary>
        Active = 1,

        /// <summary>A claim was decided and the reward is being paid. Kept only until that finishes, so a restart can finish it.</summary>
        Paying = 2
    }
}
