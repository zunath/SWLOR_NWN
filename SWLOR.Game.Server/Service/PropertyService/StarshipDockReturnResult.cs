namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum StarshipDockReturnResult
    {
        /// <summary>
        /// The ship is docked at a regular starport or an existing player starport and stays there.
        /// </summary>
        Unchanged = 0,

        /// <summary>
        /// The ship was docked in a ship-only location and returned to the starport of the planet it was at.
        /// </summary>
        ReturnedFromShipOnlyLocation = 1,

        /// <summary>
        /// The ship was docked at a player starport that no longer exists and returned to the starport of the planet
        /// it was at.
        /// </summary>
        ReturnedFromMissingStarport = 2,

        /// <summary>
        /// The ship had no dock recorded and was given the starport of the planet it was at, or its safe dock.
        /// </summary>
        RestoredMissingDock = 3,

        /// <summary>
        /// The ship had to return but had no usable safe dock recorded. It returned to the starport of the planet
        /// it was at, which is now its safe dock.
        /// </summary>
        ReturnedToPlanetStarport = 4,

        /// <summary>
        /// The ship needed to return but no safe dock could be found. It was left where it was.
        /// </summary>
        NoSafeDock = 5,
    }
}
