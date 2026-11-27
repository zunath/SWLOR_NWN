using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// What a situation's outcome does beyond the city record, gathered while its effects are applied and saved with the
    /// outcome on the city (<see cref="Entity.CitySituationOutcome"/>) before any other record changes: the planetary
    /// holding it is about, and the follow-up situations it queues.
    /// </summary>
    public sealed class CityOutcomeEffects
    {
        /// <summary>The city drives the raiders off the holding.</summary>
        public bool Help { get; set; }

        /// <summary>The credits of the holding's customs fine the treasury paid off.</summary>
        public int FineCredits { get; set; }

        /// <summary>The follow-up situations the outcome queues, created only once the city holds the outcome.</summary>
        public List<CitySituation> FollowUps { get; } = new();
    }
}
