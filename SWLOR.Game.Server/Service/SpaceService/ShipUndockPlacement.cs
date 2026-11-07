namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Where a ship docked aboard a host goes when it undocks.</summary>
    public enum ShipUndockPlacement
    {
        /// <summary>Into space beside the host, its pilot at the controls.</summary>
        Space = 0,

        /// <summary>Into the starport where the host is docked.</summary>
        Starport = 1,

        /// <summary>Refused: the host is neither in space nor at a known dock.</summary>
        NoDock = 2,

        /// <summary>Refused: the host's starport does not take a ship of the visitor's size.</summary>
        StarportTooSmall = 3,
    }
}
