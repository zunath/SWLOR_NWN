namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// Who may jump to and from a station's Jump Beacon: the mayor's choice. Allies are the citizens of the stations the
    /// mayor lists. Persisted by name on StationBuildingState.
    /// </summary>
    public enum JumpBeaconAccessType
    {
        Invalid = 0,
        Citizens = 1,
        Allies = 2,
        Anyone = 3,
    }
}
