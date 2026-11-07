namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>Why a City Project cannot be started. <see cref="None"/> means it can.</summary>
    public enum CityProjectRefusal
    {
        None = 0,

        /// <summary>The player may not start projects for the city.</summary>
        NoPermission = 1,

        /// <summary>The facility is already at its top level.</summary>
        MaxLevel = 2,

        /// <summary>The city level has to rise before the facility can.</summary>
        CityLevelTooLow = 3,

        /// <summary>The mayor's City Management perk is too low for the next level.</summary>
        MayorPerkTooLow = 4,

        /// <summary>A project of this kind is already open.</summary>
        AlreadyOpen = 5,

        /// <summary>Home Ground is already built.</summary>
        AlreadyBuilt = 6,

        /// <summary>The treasury cannot pay the credits.</summary>
        CannotAfford = 7,

        /// <summary>Only a player station has this project.</summary>
        StationOnly = 8,

        /// <summary>Not a project the city can build.</summary>
        Unknown = 9
    }
}
