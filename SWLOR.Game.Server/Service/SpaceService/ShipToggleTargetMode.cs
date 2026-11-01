namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Which target a toggled module fires at each cycle.
    /// </summary>
    public enum ShipToggleTargetMode
    {
        /// <summary>The ship's current target; follows the pilot when the target is switched.</summary>
        Follow = 0,

        /// <summary>The target selected when the module was turned on (repairers).</summary>
        Locked = 1,

        /// <summary>Works around the ship and needs no target (Bulwark, Repair Field, Capital E-War).</summary>
        SelfCentered = 2,
    }
}
