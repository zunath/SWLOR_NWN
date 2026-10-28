using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Service.SpaceService;

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

            var pool = _commonItems[tier];
            return new SlicingRewardEntry
            {
                Source = source,
                Tier = tier,
                Resref = pool[Normalize(suppliesRoll, pool.Length)],
                Name = "Recovered supplies",
                Category = SlicingRewardCategory.Common,
                IsNewDirectReward = false,
                Quantity = 3 + Normalize(suppliesRoll / pool.Length, 3)
            };
        }

        public static SlicingRewardEntry Roll(SlicingSourceType source, int tier, int categoryRoll, int itemRoll)
        {
            if (tier < 1 || tier > 5)
                throw new ArgumentOutOfRangeException(nameof(tier));

            var category = GetCategoryForRoll(categoryRoll);
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

        /// <summary>
        /// A space source's success (plan section 24): its credits at 75-125% and one bonus pick from its table.
        /// <paramref name="next"/> returns a number from 0 to its argument minus 1. A schematic pick whose pool is still
        /// empty, and an agent intel pick while <paramref name="agentIntelAvailable"/> is false, give
        /// <see cref="SpaceSlicingTuning.FallbackKind"/> instead. A source with no table (customs) gives nothing.
        /// </summary>
        public static IReadOnlyList<SpaceSlicingReward> RollSpace(
            SlicingSourceType source, int tier, Func<int, int> next, bool agentIntelAvailable = false)
        {
            var rewards = new List<SpaceSlicingReward>();
            if (!SlicingRewardCatalog.HasSpaceTable(source))
                return rewards;

            tier = Math.Clamp(tier, 1, SpaceSlicingTuning.MaximumTier);
            var baseCredits = SlicingRewardCatalog.GetSpaceCredits(source, tier);
            rewards.Add(RollSpaceCredits(baseCredits, next));

            var weights = SlicingRewardCatalog.GetSpaceBonusWeights(source)
                .Where(x => x.Value > 0)
                .OrderBy(x => (int)x.Key)
                .ToList();
            var total = weights.Sum(x => x.Value);
            if (total <= 0)
                return rewards;

            var pick = next(total);
            var kind = weights[^1].Key;
            foreach (var (candidate, weight) in weights)
            {
                if (pick < weight)
                {
                    kind = candidate;
                    break;
                }

                pick -= weight;
            }

            rewards.Add(RollSpaceKind(kind, kind, tier, baseCredits, next, agentIntelAvailable));
            return rewards;
        }

        private static SpaceSlicingReward RollSpaceCredits(int baseCredits, Func<int, int> next, SpaceSlicingRewardKind picked = SpaceSlicingRewardKind.Credits)
        {
            var span = SpaceSlicingTuning.CreditRollMaximumPercent - SpaceSlicingTuning.CreditRollMinimumPercent + 1;
            var percent = SpaceSlicingTuning.CreditRollMinimumPercent + next(span);
            return new SpaceSlicingReward
            {
                Kind = SpaceSlicingRewardKind.Credits,
                PickedKind = picked,
                Credits = Math.Max(1, baseCredits * percent / 100),
            };
        }

        private static SpaceSlicingReward RollSpaceKind(
            SpaceSlicingRewardKind kind,
            SpaceSlicingRewardKind picked,
            int tier,
            int baseCredits,
            Func<int, int> next,
            bool agentIntelAvailable)
        {
            switch (kind)
            {
                case SpaceSlicingRewardKind.Credits:
                    return RollSpaceCredits(baseCredits, next, picked);
                case SpaceSlicingRewardKind.Component:
                case SpaceSlicingRewardKind.RareComponent:
                    return RollComponent(kind, picked, tier, next);
                case SpaceSlicingRewardKind.Ammo:
                    var choices = SpaceSlicingTuning.Ammo[tier];
                    var (resref, quantity) = choices[next(choices.Length)];
                    return new SpaceSlicingReward { Kind = kind, PickedKind = picked, Resref = resref, Quantity = quantity };
                case SpaceSlicingRewardKind.DesignSchematic:
                case SpaceSlicingRewardKind.RecipeUnlock:
                case SpaceSlicingRewardKind.ShipSchematic:
                    var pool = GetSchematicPool(kind, tier);
                    if (pool.Count == 0)
                        return RollSpaceKind(SpaceSlicingTuning.FallbackKind, picked, tier, baseCredits, next, agentIntelAvailable);

                    return new SpaceSlicingReward { Kind = kind, PickedKind = picked, Resref = pool[next(pool.Count)] };
                case SpaceSlicingRewardKind.PrototypeTech:
                    return new SpaceSlicingReward { Kind = kind, PickedKind = picked, Resref = RevampRewardCatalog.PrototypeTechResref };
                case SpaceSlicingRewardKind.AgentIntel:
                    return agentIntelAvailable
                        ? new SpaceSlicingReward { Kind = kind, PickedKind = picked }
                        : RollSpaceKind(SpaceSlicingTuning.FallbackKind, picked, tier, baseCredits, next, false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown space slicing reward kind.");
            }
        }

        private static SpaceSlicingReward RollComponent(SpaceSlicingRewardKind kind, SpaceSlicingRewardKind picked, int tier, Func<int, int> next)
        {
            var families = RevampComponentCatalog.SalvageFamilies;
            var family = families[next(families.Count)];
            var traits = ShipTraitRules.Pool(family, kind == SpaceSlicingRewardKind.RareComponent ? ShipTraitKind.Rare : ShipTraitKind.Common);
            if (traits.Count == 0)
                traits = ShipTraitRules.Pool(family, ShipTraitKind.Common);

            return new SpaceSlicingReward
            {
                Kind = kind,
                PickedKind = picked,
                Resref = ShipTraitRules.ComponentResref(family, tier),
                Trait = traits[next(traits.Count)].Type,
            };
        }

        /// <summary>
        /// The schematic items a pick of a kind can give at a slicing tier (the blueprint update's items, W9b): Design
        /// and Ship Schematics for ships of the tier or lower, as salvage rare finds give them; Module Schematics (recipe
        /// unlocks of the named rank-50 modules) from <see cref="SpaceSlicingTuning.RecipeUnlockMinimumTier"/>.
        /// </summary>
        public static IReadOnlyList<string> GetSchematicPool(SpaceSlicingRewardKind kind, int tier)
        {
            tier = Math.Clamp(tier, 1, SpaceSlicingTuning.MaximumTier);
            return kind switch
            {
                SpaceSlicingRewardKind.DesignSchematic => ShipRareFindRules.Designs(tier).Select(x => x.Resref).ToList(),
                SpaceSlicingRewardKind.ShipSchematic => ShipRareFindRules.ShipSchematics(tier).Select(x => x.Resref).ToList(),
                SpaceSlicingRewardKind.RecipeUnlock => tier >= SpaceSlicingTuning.RecipeUnlockMinimumTier
                    ? RevampRewardCatalog.ModuleSchematics.Select(x => x.Resref).ToList()
                    : Array.Empty<string>(),
                _ => Array.Empty<string>(),
            };
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
