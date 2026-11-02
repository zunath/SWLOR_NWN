namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>A week a traveler spent hosted by a city. The hub is never a visit, since it hosts nobody.</summary>
    public sealed class TravelerVisit
    {
        public TravelerVisit()
        {
            CityId = string.Empty;
        }

        public TravelerVisit(string cityId, DateTime weekStartUtc)
        {
            CityId = cityId;
            WeekStartUtc = weekStartUtc;
        }

        /// <summary>The property id of the city (or station) that hosted the traveler.</summary>
        public string CityId { get; set; }

        /// <summary>The start of the week, as <see cref="CityStoreService.CityWeek"/> sets it.</summary>
        public DateTime WeekStartUtc { get; set; }
    }
}
