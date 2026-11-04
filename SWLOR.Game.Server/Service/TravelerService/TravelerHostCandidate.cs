namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>A city with a Traveler's Lodge that can host a traveler this week.</summary>
    /// <param name="CityId">The property id of the city (or station).</param>
    /// <param name="HasOpenGates">Whether the city has the Open Gates Edict in effect, which makes it twice as likely.</param>
    public readonly record struct TravelerHostCandidate(string CityId, bool HasOpenGates);
}
