namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A hunt an official called at a city's corrals (City Affairs, "Predators at the corrals"): an NPC wave stands at every
    /// corral until it is driven off or <see cref="EndsUtc"/> passes. The record lets a restart pick the hunt up again. A new
    /// type, so no migration.
    /// </summary>
    public class LivestockHunt : EntityBase
    {
        public LivestockHunt()
        {
            CityId = string.Empty;
        }

        [Indexed]
        public string CityId { get; set; }

        public DateTime StartedUtc { get; set; }

        /// <summary>When the hunt is judged: lost, with the penalty, if the wave still stands.</summary>
        public DateTime EndsUtc { get; set; }
    }
}
