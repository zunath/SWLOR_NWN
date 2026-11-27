using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityTreasuryService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A city's (or station's) treasury history: one record per city holding the last
    /// <see cref="CityTreasuryHistoryRules.KeepDays"/> days of changes, at most
    /// <see cref="CityTreasuryHistoryRules.MaxEntries"/> lines. Small frequent income (taxes, tariffs, fares and fees) is
    /// kept as one total per day, so a busy city does not grow its record with every trade. A new type, so no migration.
    /// </summary>
    public class CityTreasuryHistory : EntityBase
    {
        public CityTreasuryHistory()
        {
            CityId = string.Empty;
        }

        public CityTreasuryHistory(string cityId)
        {
            Id = KeyFor(cityId);
            CityId = cityId;
        }

        /// <summary>
        /// The DB cache holds one entity per id for the frame, whatever its type, so the id is prefixed: the bare property id
        /// is also the id of the city.
        /// </summary>
        public static string KeyFor(string cityId) => "treasury-history:" + cityId;

        /// <summary>The property id of the city or station.</summary>
        [Indexed]
        public string CityId { get; set; }

        /// <summary>The lines, oldest first.</summary>
        public List<CityTreasuryHistoryEntry> Entries { get; set; } = new();
    }
}
