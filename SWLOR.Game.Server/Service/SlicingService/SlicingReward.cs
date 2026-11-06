using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static class SlicingReward
    {
        private static readonly FrozenDictionary<int, ImmutableArray<string>> CommonItems = new Dictionary<int, string[]>
        {
            [1] = new[] { "herb_v", "kath_blood", "fiberp_ruined", "lth_ruined", "elec_ruined" },
            [2] = new[] { "herb_m", "raivor_blood", "fiberp_flawed", "lth_flawed", "elec_flawed" },
            [3] = new[] { "herb_c", "byysk_meat", "fiberp_imperfect", "lth_imperfect", "elec_imperfect" },
            [4] = new[] { "herb_t", "sanddemon_meat", "fiberp_high", "lth_high", "elec_high" },
            [5] = new[] { "herb_x", "wild_innards", "fiberp_perfect", "lth_perfect", "elec_perfect" }
        }.ToFrozenDictionary(entry => entry.Key, entry => entry.Value.ToImmutableArray());

        private static readonly ImmutableArray<ImmutableArray<string>> LegacyLockboxItems = new[]
        {
            new[] { "espn_neck_1", "espn_belt_1", "espn_ring_1" },
            new[] { "espn_neck_2", "espn_belt_2", "espn_ring_2" },
            new[] { "espn_neck_3", "espn_belt_3", "espn_ring_3" },
            new[] { "espn_neck_4", "espn_belt_4", "espn_ring_4" },
            new[] { "espn_neck_5", "espn_belt_5", "espn_ring_5" }
        }.Select(items => items.ToImmutableArray()).ToImmutableArray();

        /// <summary>
        /// Lowest tier that uses the high-tier reward weights and supply quantities. Tier 4 and 5
        /// boards are the hardest, so their bonus leans toward named items, schematics, and field
        /// notes instead of more tools.
        /// </summary>
        public const int HighTierRewardMinimumTier = 4;

        public static SlicingRewardCategory GetCategoryForRoll(int tier, int roll)
        {
            var normalized = Normalize(roll, 10000);
            if (tier >= HighTierRewardMinimumTier)
            {
                if (normalized < 5000) return SlicingRewardCategory.Tool;
                if (normalized < 8000) return SlicingRewardCategory.NamedItem;
                if (normalized < 9200) return SlicingRewardCategory.Schematic;
                return SlicingRewardCategory.FieldNote;
            }

            if (normalized < 8000) return SlicingRewardCategory.Tool;
            if (normalized < 9200) return SlicingRewardCategory.NamedItem;
            if (normalized < 9800) return SlicingRewardCategory.Schematic;
            return SlicingRewardCategory.FieldNote;
        }

        public static IReadOnlyList<SlicingRewardEntry> RollSuccess(
            SlicingSourceType source, int tier, int categoryRoll, int itemRoll, int suppliesRoll)
        {
            return new[]
            {
                RollSupplies(source, tier, suppliesRoll),
                Roll(source, tier, categoryRoll, itemRoll)
            };
        }

        private static SlicingRewardEntry RollSupplies(SlicingSourceType source, int tier, int suppliesRoll)
        {
            if (tier < 1 || tier > 5)
                throw new ArgumentOutOfRangeException(nameof(tier));

            var pool = CommonItems[tier];
            var (minimumQuantity, quantityCount) = tier >= HighTierRewardMinimumTier ? (5, 4) : (3, 3);
            return new SlicingRewardEntry
            {
                Source = source,
                Tier = tier,
                Resref = pool[Normalize(suppliesRoll, pool.Length)],
                Name = "Recovered supplies",
                Category = SlicingRewardCategory.Common,
                IsNewDirectReward = false,
                Quantity = minimumQuantity + Normalize(suppliesRoll / pool.Length, quantityCount)
            };
        }

        public static SlicingRewardEntry Roll(SlicingSourceType source, int tier, int categoryRoll, int itemRoll)
        {
            if (tier < 1 || tier > 5)
                throw new ArgumentOutOfRangeException(nameof(tier));

            var category = GetCategoryForRoll(tier, categoryRoll);
            if (category == SlicingRewardCategory.NamedItem)
                return RollNamed(source, tier, itemRoll);

            var entries = SlicingRewardCatalog.Get(source, tier, category);
            if (entries.Count == 0)
                throw new InvalidOperationException($"No {category} rewards are configured for {source} tier {tier}.");

            return entries[Normalize(itemRoll, entries.Count)];
        }

        private static SlicingRewardEntry RollNamed(SlicingSourceType source, int tier, int itemRoll)
        {
            var namedRoll = Normalize(itemRoll, 1200);
            if (namedRoll < 50)
            {
                var exceptional = SlicingRewardCatalog.Get(source, tier, SlicingRewardCategory.NamedItem, true);
                return exceptional[Normalize(itemRoll / 1200, exceptional.Count)];
            }

            if (source == SlicingSourceType.Lockbox && namedRoll < 250)
            {
                var legacy = LegacyLockboxItems[tier - 1];
                return new SlicingRewardEntry
                {
                    Source = source,
                    Tier = tier,
                    Resref = legacy[Normalize(itemRoll / 1200, legacy.Length)],
                    Name = "Legacy lockbox equipment",
                    Category = SlicingRewardCategory.NamedItem,
                    IsNewDirectReward = false
                };
            }

            var normal = SlicingRewardCatalog.Get(source, tier, SlicingRewardCategory.NamedItem, false);
            return normal[Normalize(itemRoll / 1200, normal.Count)];
        }

        private static int Normalize(int value, int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

            var normalized = value % exclusiveMaximum;
            return normalized < 0 ? normalized + exclusiveMaximum : normalized;
        }
    }
}
