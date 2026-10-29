using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The top band of ship recipes (W11o, plan section 14 and Appendix D) as pure functions, held to the W11e hull cost
    /// band (at most 1.27x a hull's pre-revamp material value):
    /// <list type="bullet">
    /// <item>A Small, Medium or Large Advanced deed has a rare-trait slot: one more component of the hull's tier, of a
    /// family its recipe already takes, carrying a rare trait. The slot is not a recipe component; the crafting window
    /// fills it.</item>
    /// <item>A Capital Advanced deed takes its maker's rare component; a tier 5 component of any salvage family carrying
    /// a rare trait may stand in for it.</item>
    /// <item>Capital deeds take the capital parts above tier 5.</item>
    /// </list>
    /// </summary>
    public static class ShipRareComponentRules
    {
        /// <summary>The crafting window's key for the rare-trait slot among a recipe's remaining components.</summary>
        public const string RareSlotKey = "#rare_trait_slot";

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
        /// The component tier a rare-traited part needs for this deed: the Advanced hull's component tier (its Piloting
        /// gate / 10 + 1, at most 5), or 0 when the resref is not an Advanced deed.
        /// </summary>
        public static int StandInTier(string deedResref)
        {
            if (deedResref == null || !_advancedHulls.TryGetValue(deedResref, out var hull))
                return 0;

            return System.Math.Min(5, hull.PilotingRank / 10 + 1);
        }

        /// <summary>Whether the deed has a rare-trait slot: a Small, Medium or Large Advanced hull.</summary>
        public static bool HasRareSlot(string deedResref) =>
            deedResref != null && _advancedHulls.TryGetValue(deedResref, out var hull) && hull.Size != ShipSize.Capital;

        /// <summary>
        /// The salvage families whose rare-traited component may fill the deed's rare slot or stand in for its rare
        /// component: any on a Capital hull, otherwise the families its recipe already takes.
        /// </summary>
        public static IReadOnlyList<ShipComponentFamily> StandInFamilies(string deedResref)
        {
            if (deedResref == null || !_advancedHulls.TryGetValue(deedResref, out var hull))
                return new List<ShipComponentFamily>();

            if (hull.Size == ShipSize.Capital)
                return RevampComponentCatalog.SalvageFamilies.ToList();

            var families = hull.Components.Select(x => ShipTraitRules.FamilyOf(x.Resref)).ToHashSet();
            return RevampComponentCatalog.SalvageFamilies.Where(families.Contains).ToList();
        }

        /// <summary>The rare component the deed recipe takes, or null.</summary>
        public static string RareComponentOf(IEnumerable<string> recipeComponents)
        {
            return recipeComponents?.FirstOrDefault(IsRareComponent);
        }

        /// <summary>
        /// Whether an item can fill the deed's rare slot or stand in for its rare component: a generic component of the
        /// hull's tier and one of its stand-in families, carrying at least one rare trait.
        /// </summary>
        public static bool CanStandIn(string deedResref, string resref, IEnumerable<ShipComponentTraitType> traits)
        {
            var tier = StandInTier(deedResref);
            if (tier <= 0 || !ShipTraitRules.IsComponent(resref) || IsRareComponent(resref))
                return false;

            var item = RevampComponentCatalog.Items.First(x => x.Resref == resref);
            if (item.Tier != tier || !StandInFamilies(deedResref).Contains(item.Family))
                return false;

            return (traits ?? Enumerable.Empty<ShipComponentTraitType>())
                .Any(x => ShipTraitRules.Get(x)?.Kind == ShipTraitKind.Rare);
        }

        /// <summary>
        /// How many rare-traited parts the deed still needs from what is held: its rare slot (1), or the rare components
        /// the held ones do not cover.
        /// </summary>
        public static int StandInsNeeded(string deedResref, IReadOnlyDictionary<string, int> recipeComponents,
            IEnumerable<(string Resref, int Stack)> held)
        {
            if (StandInTier(deedResref) <= 0)
                return 0;

            if (HasRareSlot(deedResref))
                return 1;

            var rare = RareComponentOf(recipeComponents.Keys);
            return rare == null ? 0 : System.Math.Max(0, recipeComponents[rare] - held.Where(x => x.Resref == rare).Sum(x => x.Stack));
        }

        /// <summary>
        /// Picks the held items that fill the deed's rare slot or stand in for its missing rare components, one item
        /// each. Items the recipe does not otherwise need go first; an item whose resref the recipe needs is used only
        /// from the surplus, so the recipe can still be completed. Returns the indexes into <paramref name="held"/>.
        /// </summary>
        public static List<int> ReserveStandIns(
            string deedResref,
            IReadOnlyDictionary<string, int> recipeComponents,
            IReadOnlyList<(string Resref, int Stack, IReadOnlyCollection<ShipComponentTraitType> Traits)> held)
        {
            var reserved = new List<int>();
            var missing = StandInsNeeded(deedResref, recipeComponents, held.Select(x => (x.Resref, x.Stack)));
            if (missing <= 0)
                return reserved;

            var available = held.GroupBy(x => x.Resref).ToDictionary(x => x.Key, x => x.Sum(y => y.Stack));
            var candidates = Enumerable.Range(0, held.Count)
                .Where(i => CanStandIn(deedResref, held[i].Resref, held[i].Traits))
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

        /// <summary>The line the recipe window adds under an Advanced deed's components, or null for other recipes.</summary>
        public static string StandInText(string deedResref, string rareName)
        {
            var tier = StandInTier(deedResref);
            if (tier <= 0)
                return null;

            if (!HasRareSlot(deedResref))
                return $"(or, for the {rareName}, a tier {tier} component with a rare trait)";

            var families = string.Join(", ", StandInFamilies(deedResref).Select(FamilyName));
            return $"1x Tier {tier} component with a rare trait ({families})";
        }

        /// <summary>The characters a recipe detail row shows before the craft window cuts it off.</summary>
        public const int DetailLineCharacters = 44;

        /// <summary>
        /// <see cref="StandInText"/> wrapped at word breaks onto recipe detail rows, which are single-line labels; the
        /// whole line ("...Thruster Assembly, Energy Emitter)") was cut off even in a widened window (W11r). Empty for
        /// other recipes.
        /// </summary>
        public static List<string> StandInLines(string deedResref, string rareName)
        {
            var lines = new List<string>();
            var text = StandInText(deedResref, rareName);
            if (text == null)
                return lines;

            var current = string.Empty;
            foreach (var word in text.Split(' '))
            {
                if (current.Length > 0 && current.Length + 1 + word.Length > DetailLineCharacters)
                {
                    lines.Add(current);
                    current = "  " + word;
                }
                else
                    current = current.Length == 0 ? word : current + " " + word;
            }

            lines.Add(current);
            return lines;
        }

        private static string FamilyName(ShipComponentFamily family)
        {
            var name = RevampComponentCatalog.Items.First(x => x.Family == family).Name;
            var paren = name.IndexOf(" (", System.StringComparison.Ordinal);
            return paren > 0 ? name.Substring(0, paren) : name;
        }
    }
}
