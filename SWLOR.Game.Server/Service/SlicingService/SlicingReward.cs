using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static class SlicingReward
    {
        private static readonly Dictionary<int, string[]> _commonItems = new()
        {
            [1] = new[] { "herb_v", "kath_blood", "fiberp_ruined", "lth_ruined", "elec_ruined" },
            [2] = new[] { "herb_m", "raivor_blood", "fiberp_flawed", "lth_flawed", "elec_flawed" },
            [3] = new[] { "herb_c", "byysk_meat", "fiberp_imperfect", "lth_imperfect", "elec_imperfect" },
            [4] = new[] { "herb_t", "sanddemon_meat", "fiberp_high", "lth_high", "elec_high" },
            [5] = new[] { "herb_x", "wild_innards", "fiberp_perfect", "lth_perfect", "elec_perfect" }
        };

        private static readonly string[][] _legacyLockboxItems =
        {
            new[] { "espn_neck_1", "espn_belt_1", "espn_ring_1" },
            new[] { "espn_neck_2", "espn_belt_2", "espn_ring_2" },
            new[] { "espn_neck_3", "espn_belt_3", "espn_ring_3" },
            new[] { "espn_neck_4", "espn_belt_4", "espn_ring_4" },
            new[] { "espn_neck_5", "espn_belt_5", "espn_ring_5" }
        };

        public static SlicingRewardCategory GetCategoryForRoll(int roll)
        {
            var normalized = Normalize(roll, 10000);
            if (normalized < 6500) return SlicingRewardCategory.Common;
            if (normalized < 8000) return SlicingRewardCategory.Tool;
            if (normalized < 9200) return SlicingRewardCategory.NamedItem;
            if (normalized < 9800) return SlicingRewardCategory.Schematic;
            return SlicingRewardCategory.FieldNote;
        }

        public static SlicingRewardEntry Roll(SlicingSourceType source, int tier, int categoryRoll, int itemRoll)
        {
            if (tier < 1 || tier > 5)
                throw new ArgumentOutOfRangeException(nameof(tier));

            var category = GetCategoryForRoll(categoryRoll);
            if (category == SlicingRewardCategory.Common)
            {
                var pool = _commonItems[tier];
                return new SlicingRewardEntry
                {
                    Source = source,
                    Tier = tier,
                    Resref = pool[Normalize(itemRoll, pool.Length)],
                    Name = "Recovered supplies",
                    Category = category,
                    IsNewDirectReward = false,
                    Quantity = 1 + Normalize(itemRoll / Math.Max(1, pool.Length), 3)
                };
            }

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
                var legacy = _legacyLockboxItems[tier - 1];
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
