namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>
    /// What a City Project builds (Player City Revamp, "City Projects"). Values are saved with the project and never change.
    /// </summary>
    public enum CityProjectType
    {
        Invalid = 0,

        /// <summary>The next level of the city's Medical Centers.</summary>
        MedicalCenterUpgrade = 1,

        /// <summary>The next level of the city's Starports.</summary>
        StarportUpgrade = 2,

        /// <summary>The next level of the city's Cantinas.</summary>
        CantinaUpgrade = 3,

        /// <summary>The next level of a player station's Jump Beacon.</summary>
        JumpBeaconUpgrade = 4,

        /// <summary>Home Ground, built once per city or station.</summary>
        HomeGround = 5
    }
}
