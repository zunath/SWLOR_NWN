using System.Collections.Generic;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.FarmhandService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A droid its owner assigned to one of their houses or to a corral (Player City Revamp, "Farmhands"). In a house it tends
    /// its owner's crops; at a corral it feeds and collects from its owner's animals, working from a feed hopper and an output
    /// hopper of 20 items each. The droid's controller is kept here, as a station building keeps its droid. The id is
    /// <see cref="KeyFor"/> of the target's property id, never the property id itself, since the DB's per-frame entity cache is
    /// keyed by id alone. A new type, so no migration.
    /// </summary>
    public class Farmhand : EntityBase
    {
        public Farmhand()
        {
            TargetId = string.Empty;
            CityId = string.Empty;
            OwnerPlayerId = string.Empty;
            DroidName = string.Empty;
            SerializedDroid = string.Empty;
            DroidAssignmentId = string.Empty;
            FeedHopper = new Dictionary<CropType, int>();
            OutputHopper = new Dictionary<string, int>();
        }

        /// <summary>The farmhand's key for a house's or corral's property id.</summary>
        public static string KeyFor(string targetId) => "farmhand:" + targetId;

        /// <summary>The house or corral the droid works.</summary>
        [Indexed]
        public string TargetId { get; set; }

        public FarmhandTargetType TargetType { get; set; }

        /// <summary>The city the house or corral stands in. A farmhand is brought up to date when someone enters the city's area.</summary>
        [Indexed]
        public string CityId { get; set; }

        /// <summary>The character who assigned the droid. It works only their crops and animals.</summary>
        [Indexed]
        public string OwnerPlayerId { get; set; }

        /// <summary>The droid's controller item (ObjectPlugin.Serialize).</summary>
        public string SerializedDroid { get; set; }

        public string DroidName { get; set; }

        /// <summary>The droid's tier (1 to 5), read when it was assigned. It sets the highest crop and animal tier it handles.</summary>
        public int DroidTier { get; set; }

        /// <summary>The assignment of the droid, matching the mark on its controller while a restart could leave it in the owner's character file too.</summary>
        public string DroidAssignmentId { get; set; }

        /// <summary>The crops loaded as feed, by crop. At most <see cref="FarmhandRules.HopperCapacity"/> in all.</summary>
        public Dictionary<CropType, int> FeedHopper { get; set; }

        /// <summary>What the droid collected, by item resref, until the owner empties it. At most <see cref="FarmhandRules.HopperCapacity"/> in all.</summary>
        public Dictionary<string, int> OutputHopper { get; set; }

        /// <summary>The last time the droid's work was brought up to date. Its replay starts here.</summary>
        public DateTime LastProcessedUtc { get; set; }
    }
}
