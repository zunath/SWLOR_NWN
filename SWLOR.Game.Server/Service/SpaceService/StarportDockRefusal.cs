namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a starport cannot take a ship (plan section 7, "Docking limits by size").</summary>
    public enum StarportDockRefusal
    {
        None = 0,

        /// <summary>The ship has no hull size (bad data).</summary>
        UnknownSize = 1,

        /// <summary>The dock's area admits only smaller hulls (a ship-only location such as the Abandoned Station).</summary>
        AreaSizeLimit = 2,

        /// <summary>A Capital hull at an NPC dock that is not one of the major ports.</summary>
        NpcDockTooSmall = 3,

        /// <summary>A Capital hull at a player starport below level 2.</summary>
        PlayerStarportTooSmall = 4,
    }
}
