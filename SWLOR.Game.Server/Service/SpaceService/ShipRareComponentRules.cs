using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The top band of ship recipes (W11o, plan section 14 and Appendix D) as pure functions: the rare component an
    /// Advanced deed recipe takes and the salvaged components that may stand in for it, and the capital parts that
    /// Capital deed recipes take above tier 5.
    /// </summary>
    public static class ShipRareComponentRules
    {
        private static readonly HashSet<string> _rareResrefs = RevampComponentCatalog.Items
            .Where(x => RevampComponentCatalog.RareFamilies.Contains(x.Family))
            .Select(x => x.Resref)
            .ToHashSet();

        private static readonly HashSet<string> _capitalParts = RevampComponentCatalog.CapitalPartRecipes
            .Select(x => x.Resref)
            .ToHashSet();

        private static readonly Dictionary<string, ShipHullEntry> _advancedHulls = RevampShipCatalog.Hulls
            .Where(x => x.Advanced)
            .ToDictionary(x => x.Tag);

        /// <summary>Whether the resref is a rare component (Thorilide Core, Aurodium Plating, Prototype Core).</summary>
        public static bool IsRareComponent(string resref) => resref != null && _rareResrefs.Contains(resref);

        /// <summary>Whether the resref is a capital part, the top band Capital hulls take.</summary>
        public static bool IsCapitalPart(string resref) => resref != null && _capitalParts.Contains(resref);

        /// <summary>
        /// The lowest component tier a stand-in needs for this deed: the Advanced hull's component tier (its Piloting gate
        /// / 10 + 1, at most 5), or 0 when the resref is not an Advanced deed.
        /// </summary>
        public static int StandInTier(string deedResref)
        {
            if (deedResref == null || !_advancedHulls.TryGetValue(deedResref, out var hull))
                return 0;

            return System.Math.Min(5, hull.PilotingRank / 10 + 1);
        }

        /// <summary>The rare component the deed recipe takes, or null.</summary>
        public static string RareComponentOf(IEnumerable<string> recipeComponents)
        {
            return recipeComponents?.FirstOrDefault(IsRareComponent);
        }

        /// <summary>
        /// Whether an item can stand in for a rare component: a generic component (not a rare one) of at least the tier,
        /// carrying at least one rare trait.
        /// </summary>
        public static bool CanStandIn(string resref, IEnumerable<ShipComponentTraitType> traits, int minimumTier)
        {
            if (minimumTier <= 0 || !ShipTraitRules.IsComponent(resref) || IsRareComponent(resref))
                return false;

            var item = RevampComponentCatalog.Items.First(x => x.Resref == resref);
            if (item.Tier < minimumTier)
                return false;

            return (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Any(x => ShipTraitRules.Get(x)?.Kind == ShipTraitKind.Rare);
        }

        /// <summary>
        /// Picks the held items that stand in for the recipe's missing rare components, one item each. Items the recipe
        /// does not otherwise need go first; an item whose resref the recipe needs is used only from the surplus, so the
        /// recipe can still be completed. Returns the indexes into <paramref name="held"/>.
        /// </summary>
        public static List<int> ReserveStandIns(
            IReadOnlyDictionary<string, int> recipeComponents,
            IReadOnlyList<(string Resref, int Stack, IReadOnlyCollection<ShipComponentTraitType> Traits)> held,
            int minimumTier)
        {
            var reserved = new List<int>();
            var rare = RareComponentOf(recipeComponents.Keys);
            if (rare == null || minimumTier <= 0)
                return reserved;

            var missing = recipeComponents[rare] - held.Where(x => x.Resref == rare).Sum(x => x.Stack);
            if (missing <= 0)
                return reserved;

            var available = held.GroupBy(x => x.Resref).ToDictionary(x => x.Key, x => x.Sum(y => y.Stack));
            var candidates = Enumerable.Range(0, held.Count)
                .Where(i => CanStandIn(held[i].Resref, held[i].Traits, minimumTier))
                .OrderBy(i => recipeComponents.ContainsKey(held[i].Resref) ? 1 : 0)
                .ToList();

            foreach (var index in candidates)
            {
                if (missing <= 0)
                    break;

                var resref = held[index].Resref;
                if (recipeComponents.TryGetValue(resref, out var needed) && available[resref] - 1 < needed)
                    continue;

                available[resref]--;
                reserved.Add(index);
                missing--;
            }

            return reserved;
        }

        /// <summary>The line the recipe window adds under an Advanced deed's components.</summary>
        public static string StandInText(int minimumTier, string rareName)
        {
            return $"(or, for the {rareName}, any tier {minimumTier}+ component with a rare trait)";
        }
    }
}
