using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// An animal standing in a corral (Player City Revamp, "Farming: Livestock"). A crated animal has no record: it is an
    /// item that holds its timers as remaining durations. The record belongs to its owner: it counts toward their CD key's six
    /// animals, and only they feed, collect from, butcher or crate it. A new type, so no migration.
    /// </summary>
    public class Livestock : EntityBase
    {
        public Livestock()
        {
            OwnerPlayerId = string.Empty;
            OwnerCdKey = string.Empty;
            CorralId = string.Empty;
        }

        /// <summary>The character who placed the animal.</summary>
        [Indexed]
        public string OwnerPlayerId { get; set; }

        /// <summary>The owner's public CD key. The six-animal cap counts every character that shares it.</summary>
        [Indexed]
        public string OwnerCdKey { get; set; }

        /// <summary>The corral structure's property id.</summary>
        [Indexed]
        public string CorralId { get; set; }

        public LivestockType Species { get; set; }

        /// <summary>When the animal was placed. An animal that was crated and placed again has this moved back by the age it had.</summary>
        public DateTime PlacedUtc { get; set; }

        /// <summary>How many times the animal has been fed.</summary>
        public int Feedings { get; set; }

        /// <summary>The last meal, or null for an animal never fed. An animal unfed for 72 hours wanders off.</summary>
        public DateTime? LastFedUtc { get; set; }

        /// <summary>The last collection of manure, or null for an animal never collected from.</summary>
        public DateTime? LastCollectedUtc { get; set; }

        /// <summary>The predators' penalty: the animal gives no products until this time. Null when it has none.</summary>
        public DateTime? NoProductsUntilUtc { get; set; }

        /// <summary>The last time the animal was brought up to date. Farmhands replay time from here.</summary>
        public DateTime LastProcessedUtc { get; set; }
    }
}
