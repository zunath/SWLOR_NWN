namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>What happens to a ship docked at a player Starport when its owner's access is checked.</summary>
    public enum DockedShipAction
    {
        /// <summary>Nothing changes.</summary>
        Leave = 0,

        /// <summary>The owner may use the Starport, so their 7-day departure pass starts over.</summary>
        RefreshPass = 1,

        /// <summary>The owner may not use the Starport and holds no pass, so their 7 days start now.</summary>
        StartPass = 2,

        /// <summary>The owner lost access and their departure pass ran out, so the ship goes to its safe dock.</summary>
        ReturnToSafeDock = 3
    }
}
