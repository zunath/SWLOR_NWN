namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// The part a player plays in running a city (Player City Revamp, "Acting mayor and stepping down"). It decides which
    /// powers of the Plan's Permissions table the player has before any granted permission is counted.
    /// </summary>
    public enum CityRole
    {
        /// <summary>A citizen or anyone else. Only a granted permission gives powers.</summary>
        None = 0,

        /// <summary>The sitting mayor, with every power.</summary>
        Mayor = 1,

        /// <summary>
        /// The Acting Mayor the mayor named, or the interim holder after the mayor stepped down. Both have the Acting
        /// Mayor's powers.
        /// </summary>
        ActingMayor = 2,

        /// <summary>An officer the mayor appointed.</summary>
        Officer = 3,

        /// <summary>
        /// The mayor who stepped down. They stay mayor of record, so the city's level still follows their rank, but they
        /// have no powers until the snap election ends.
        /// </summary>
        DepartedMayor = 4
    }
}
