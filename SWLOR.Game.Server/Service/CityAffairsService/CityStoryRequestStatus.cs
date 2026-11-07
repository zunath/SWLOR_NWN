namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// Where a city's story request stands with the DMs. Values are saved on the city and never change.
    /// </summary>
    public enum CityStoryRequestStatus
    {
        Invalid = 0,

        /// <summary>Posted and waiting for a DM.</summary>
        Open = 1,

        /// <summary>A DM took it and plans to run it.</summary>
        Taken = 2,

        /// <summary>A DM ran it, or closed it.</summary>
        Done = 3
    }
}
