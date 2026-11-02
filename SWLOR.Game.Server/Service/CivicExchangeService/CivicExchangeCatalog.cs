using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CivicExchangeService
{
    /// <summary>
    /// What the Civic Exchange sells and for how much Available XP (Player City Revamp, "Civic Exchange"). This is the one place
    /// the stock and prices live, as data, and every number here is a proposal for the owner's review: the Plan says staff draft
    /// them and players review them before release. The evidence for each number is in the pull request that added them.
    ///
    /// Stock opens with the Cantina level: tier N materials at level N, and the long buff at level 3. Cosmetic unlocks (container
    /// and weapon appearances) are not here: no cosmetic unlock system exists to hang them on, so they are a listed follow-up.
    /// Research time and faster training are not sold, by the Plan.
    /// </summary>
    public static class CivicExchangeCatalog
    {
        // The materials crates

        /// <summary>The most crates one CD key buys in a week, across all its characters and every tier.</summary>
        public const int WeeklyCrateLimit = 3;

        /// <summary>How many raw materials a crate holds, each rolled from its tier's harvest tables.</summary>
        public const int CrateUnits = 30;

        /// <summary>
        /// Seconds of active harvesting that one harvested unit stands for: the crafting revamp's progression baseline prices a
        /// harvested unit at 20 seconds. A crate must stay under 20 minutes of harvesting at its tier.
        /// </summary>
        public const int HarvestSecondsPerUnit = 20;

        /// <summary>The most minutes of harvesting a crate may be worth (the Plan's passive output cap).</summary>
        public const int MaxCrateHarvestMinutes = 20;

        /// <summary>
        /// The harvest tables a crate of each tier rolls: the same ore, wood, fiber and herb tables that harvesting that tier pays
        /// from, so a crate holds exactly the materials the tier's nodes give. Each unit picks one table at random, then rolls it.
        /// </summary>
        private static readonly IReadOnlyDictionary<int, string[]> CrateTables = new Dictionary<int, string[]>
        {
            [1] = new[] { "HARVESTING_VELDITE", "HARVESTING_TREE", "SCAVENGING_BUSH_1", "HERBS_1" },
            [2] = new[] { "HARVESTING_SCORDSPAR", "HARVESTING_OAK_TREE", "SCAVENGING_BUSH_2", "HERBS_2" },
            [3] = new[] { "HARVESTING_PLAGIONITE", "HARVESTING_ANCIENT_TREE", "SCAVENGING_BUSH_3", "HERBS_3" },
            [4] = new[] { "HARVESTING_KEROMBER", "HARVESTING_ARACIA_TREE", "SCAVENGING_BUSH_4", "HERBS_4" },
            [5] = new[] { "HARVESTING_JASIOCLASE", "HARVESTING_HYPHAE_TREE", "SCAVENGING_BUSH_5", "HERBS_5" }
        };

        // The long buff: each stat is no more than the best tier-5 food gives for it (Dath Herb Soup 25 FP, Echani Chili-Oil
        // Noodles 25 stamina, Sando g'dizzards 17 physical defense, Long Ling Lung 10 force defense), and the whole buff is smaller
        // than Popper Bowl (30 HP, 20 FP, 15 stamina). It holds no HP, so it never touches the temporary HP food keeps.

        /// <summary>The buff's duration: 2 hours, against the 30 to 45 minutes of a food buff.</summary>
        public const int BuffDurationSeconds = 2 * 60 * 60;

        public const int BuffMaxFP = 20;
        public const int BuffMaxStamina = 15;
        public const int BuffPhysicalDefense = 5;
        public const int BuffForceDefense = 5;

        public const string BuffName = "Civic Provisions";

        /// <summary>The buff's one-line effect, for its tooltip and the application message.</summary>
        public static string BuffSummary =>
            $"+{BuffMaxFP} max FP, +{BuffMaxStamina} max stamina, +{BuffPhysicalDefense} physical defense and +{BuffForceDefense} force defense for 2 hours. " +
            "Eating food replaces it, and it replaces any food you are under.";

        private static readonly CivicExchangeItem[] _items =
        {
            Crate(1, 1000, "iit_smlmisc_153"),
            Crate(2, 1500, "iit_smlmisc_156"),
            Crate(3, 2000, "iit_smlmisc_152"),
            Crate(4, 2500, "iit_smlmisc_154"),
            Crate(5, 3000, "iit_smlmisc_155"),
            new CivicExchangeItem(
                "buff_civic_provisions",
                CivicExchangeItemKind.Buff,
                BuffName,
                BuffSummary,
                "iit_smlmisc_169",
                2500,
                3,
                0)
        };

        public static IReadOnlyList<CivicExchangeItem> All => _items;

        /// <summary>The item with the key, or null.</summary>
        public static CivicExchangeItem Get(string key)
        {
            return _items.FirstOrDefault(x => x.Key == key);
        }

        /// <summary>Every item a Cantina of this level stocks, in the order the window lists them.</summary>
        public static IReadOnlyList<CivicExchangeItem> GetStock(int cantinaLevel)
        {
            return _items.Where(x => x.MinCantinaLevel <= cantinaLevel).ToList();
        }

        /// <summary>The harvest tables a crate of the tier rolls, or none for a tier with no crate.</summary>
        public static IReadOnlyList<string> GetCrateTables(int tier)
        {
            return CrateTables.TryGetValue(tier, out var tables) ? tables : System.Array.Empty<string>();
        }

        /// <summary>The key of the crate of a tier.</summary>
        public static string GetCrateKey(int tier) => $"crate_t{tier}";

        private static CivicExchangeItem Crate(int tier, int priceXp, string icon)
        {
            return new CivicExchangeItem(
                GetCrateKey(tier),
                CivicExchangeItemKind.MaterialsCrate,
                $"Tier {tier} Materials Crate",
                $"{CrateUnits} random tier {tier} materials: ore, wood, fiber and herbs, rolled from the same tables that harvesting tier {tier} nodes pay from. " +
                $"Each CD key buys {WeeklyCrateLimit} crates a week.",
                icon,
                priceXp,
                tier,
                tier);
        }
    }
}
