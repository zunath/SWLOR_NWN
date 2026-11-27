using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>The aces (plan section 11) as pure lookups and the spawn share rule.</summary>
    public static class ShipAceRules
    {
        /// <summary>The creature local an ace carries; its wreck keeps a data core for slicing (plan section 12).</summary>
        public const string AceVariable = "SPACE_ACE";

        private static readonly Dictionary<string, ShipAce> _byTag = RevampAceCatalog.Aces.ToDictionary(x => x.Tag);

        /// <summary>The ace with this creature tag, or null.</summary>
        public static ShipAce Get(string creatureTag) =>
            creatureTag != null && _byTag.TryGetValue(creatureTag, out var ace) ? ace : null;

        public static bool IsAce(string creatureTag) => Get(creatureTag) != null;

        /// <summary>
        /// Whether an ace's weight is a ground rare's share of its spawn table: a weight-1 rare beside about 120 weight of
        /// normal spawns rolls 0.8% of the time, so an ace's weight over the table's total must land in the catalog's band.
        /// </summary>
        public static bool IsRareShare(int aceWeight, int tableWeight) =>
            tableWeight > 0 &&
            aceWeight / (double)tableWeight >= RevampAceCatalog.MinimumSpawnShare &&
            aceWeight / (double)tableWeight <= RevampAceCatalog.MaximumSpawnShare;
    }
}
