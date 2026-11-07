namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>Where a traveler is this week, as players are told.</summary>
    /// <param name="Type">The traveler.</param>
    /// <param name="CityName">The host city, or an empty string when the traveler waits at the hub.</param>
    /// <param name="AreaName">The area the city stands in, or an empty string when it is not known.</param>
    public readonly record struct TravelerLocation(TravelerType Type, string CityName, string AreaName);
}
