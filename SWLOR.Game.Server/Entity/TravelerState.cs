using System.Collections.Generic;
using SWLOR.Game.Server.Service.TravelerService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Where one traveler is: the city hosting it this week (empty while it waits at the hub), the week that was chosen, and
    /// the weeks it was hosted before, so a city does not host the same traveler again within 4 weeks. One record per
    /// traveler, with the name of its <see cref="TravelerType"/> member as the id. A new type, so no migration.
    /// </summary>
    public class TravelerState : EntityBase
    {
        public TravelerState()
        {
            HostCityId = string.Empty;
            History = new List<TravelerVisit>();
        }

        public TravelerState(TravelerType type) : this()
        {
            Id = type.ToString();
        }

        /// <summary>The property id of the host city or station, or an empty string for the hub.</summary>
        public string HostCityId { get; set; }

        /// <summary>The start of the week the host was chosen for. The traveler moves when a new week begins.</summary>
        public DateTime RotatedWeekStartUtc { get; set; }

        /// <summary>The weeks it was hosted, newest last. Kept for a few weeks longer than the repeat rule needs.</summary>
        public List<TravelerVisit> History { get; set; }
    }
}
