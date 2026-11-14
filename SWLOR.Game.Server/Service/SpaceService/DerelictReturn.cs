namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Where a boarder goes when they leave a derelict, or when its boarding ends with them aboard.</summary>
    public enum DerelictReturn
    {
        /// <summary>Their ship's interior, wherever the ship is.</summary>
        ShipInterior = 0,

        /// <summary>The ship cannot take them (its interior is not ready, or it is gone): the dock it is towed to.</summary>
        ShipDock = 1,

        /// <summary>Neither is reachable: the module's starting location.</summary>
        Start = 2,
    }
}
