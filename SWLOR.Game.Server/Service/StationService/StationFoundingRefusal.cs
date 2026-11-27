namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>Why a Station Core cannot found a station, in the order <see cref="StationRules.GetFoundingRefusal"/> checks.</summary>
    public enum StationFoundingRefusal
    {
        None = 0,
        NotInSpace = 1,
        NoAnchorInRange = 2,
        AreaSizeLimit = 3,
        SiteTaken = 4,
        AlreadyCitizen = 5,
        MissingCityManagement = 6,
        InsufficientCredits = 7,
    }
}
