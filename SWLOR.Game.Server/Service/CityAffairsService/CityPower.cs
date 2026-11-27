namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// A row of the Plan's Permissions table: something an official can do for a city. <see cref="CityPowerRules"/> says
    /// which roles hold each power.
    /// </summary>
    public enum CityPower
    {
        Invalid = 0,

        /// <summary>Appoint officers or an Acting Mayor, and step down.</summary>
        Appoint = 1,

        /// <summary>Grant city permissions in the permission window.</summary>
        GrantPermissions = 2,

        /// <summary>Decide City Affairs situations.</summary>
        HandleCityAffairs = 3,

        /// <summary>Enact Edicts into empty slots.</summary>
        EnactEdicts = 4,

        /// <summary>Repeal Edicts.</summary>
        RepealEdicts = 5,

        /// <summary>Change taxes and fees.</summary>
        ChangeTaxes = 6,

        /// <summary>Withdraw from the treasury.</summary>
        WithdrawTreasury = 7,

        /// <summary>Pay upkeep.</summary>
        PayUpkeep = 8,

        /// <summary>Start City Projects. Until they exist, this covers the facility upgrades they replace.</summary>
        StartProjects = 9,

        /// <summary>Post bounties, tournaments, and story requests.</summary>
        PostRequests = 10
    }
}
