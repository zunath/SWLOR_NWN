namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>A named place on a station deck: a civic building's slot, or one kept clear for a station-only building.</summary>
    public enum StationDeckSlotType
    {
        Invalid = 0,
        Core = 1,
        Starport = 2,
        MedicalCenter = 3,
        Bank = 4,
        Cantina = 5,
        Shipyard = 6,
        AsteroidDrill = 7,
        JumpBeacon = 8,
        StationFuel = 9,
    }
}
