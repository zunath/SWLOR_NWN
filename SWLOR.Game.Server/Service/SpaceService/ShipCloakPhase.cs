namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Where a ship is in the Cloaking Device's cycle.</summary>
    public enum ShipCloakPhase
    {
        /// <summary>No cloak: the ship is visible.</summary>
        Off = 0,

        /// <summary>The device is on and the ship is still visible; it vanishes when the cloak time ends.</summary>
        Engaging = 1,

        /// <summary>The ship is hidden from everyone who cannot reveal it.</summary>
        Cloaked = 2,
    }
}
