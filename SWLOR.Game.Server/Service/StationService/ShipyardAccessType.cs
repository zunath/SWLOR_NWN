namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>Who may build at a station's Shipyard: the mayor's choice. Persisted by name on StationBuildingState.</summary>
    public enum ShipyardAccessType
    {
        Invalid = 0,
        Citizens = 1,
        Anyone = 2,
    }
}
