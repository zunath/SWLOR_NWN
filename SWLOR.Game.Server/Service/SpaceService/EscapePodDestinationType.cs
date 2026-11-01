namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Where the escape pods of a destroyed ship put their passengers.
    /// </summary>
    public enum EscapePodDestinationType
    {
        /// <summary>No dock could be found. The passengers are left to the normal death flow.</summary>
        None = 0,

        /// <summary>The dock the ship is towed to.</summary>
        Dock = 1,

        /// <summary>The dock is a player starport that is not loaded; the ship last NPC dock receives the pods.</summary>
        SafeDock = 2,
    }
}
