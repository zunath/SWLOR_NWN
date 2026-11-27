using SWLOR.Game.Server.Service.CityGazetteService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// One line of a city's public log. Each city keeps its newest <see cref="CityGazetteRules.MaxEntriesPerCity"/> entries.
    /// A new type, so no migration.
    /// </summary>
    public class CityGazetteEntry : EntityBase
    {
        public CityGazetteEntry()
        {
            CityId = string.Empty;
            Text = string.Empty;
            OfficialPlayerId = string.Empty;
            OfficialTitle = string.Empty;
        }

        /// <summary>The property id of the city (or station) the entry belongs to.</summary>
        [Indexed]
        public string CityId { get; set; }

        /// <summary>When the entry was posted.</summary>
        public DateTime TimeUtc { get; set; }

        public CityGazetteEntryType EntryType { get; set; }

        /// <summary>The entry as readers see it. Holds no player names.</summary>
        public string Text { get; set; }

        /// <summary>The official the entry names, or empty. Readers see the name through <c>PlayerName</c>.</summary>
        public string OfficialPlayerId { get; set; }

        /// <summary>The official's title when they acted, such as Mayor, or empty.</summary>
        public string OfficialTitle { get; set; }

        /// <summary>A secret entry is kept out of the public log and the Discord feed until revealed.</summary>
        public bool IsSecret { get; set; }

        /// <summary>A hook entry is also sent to the DM feed as a story hook.</summary>
        public bool IsHook { get; set; }
    }
}
