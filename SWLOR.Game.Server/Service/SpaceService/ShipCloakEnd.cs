namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What ending a cloak did.</summary>
    public enum ShipCloakEnd
    {
        /// <summary>The ship had no cloak.</summary>
        None = 0,

        /// <summary>The cloak ended before the ship vanished, so nothing is locked.</summary>
        Cancelled = 1,

        /// <summary>The ship became visible again and its weapons are locked.</summary>
        Decloaked = 2,
    }
}
