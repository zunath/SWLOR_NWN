namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>Why a player cannot build a Capital hull here, from <see cref="StationBuildingRules.GetShipyardRefusal"/>.</summary>
    public enum ShipyardRefusal
    {
        None = 0,
        NotAtShipyard = 1,
        CitizensOnly = 2,
        OwesTaxes = 3,

        /// <summary>The station has an Embargo against the player's city.</summary>
        Embargoed = 4,
    }
}
