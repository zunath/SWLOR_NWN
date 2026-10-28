using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Ship component traits on items (plan section 14). A trait is stored as an item local
    /// (<see cref="ShipTraitRules.TraitsVariable"/>) and named in brackets after the item's name. Crafted variants stamp
    /// their trait on the component; a revamp module or ship deed crafted from components inherits every trait they
    /// carried, and a registered ship keeps its deed's traits as hull traits.
    /// </summary>
    public static class ShipComponents
    {
        private static readonly HashSet<string> _hullTags = RevampShipCatalog.Hulls.Select(x => x.Tag).ToHashSet();

        /// <summary>Whether the resref is a revamp ship deed, whose traits become its ship's hull traits.</summary>
        public static bool IsHullDeed(string resref) => resref != null && _hullTags.Contains(resref);

        /// <summary>The traits an item carries.</summary>
        public static List<ShipComponentTraitType> GetTraits(uint item)
        {
            return ShipTraitRules.Parse(GetLocalString(item, ShipTraitRules.TraitsVariable));
        }

        /// <summary>The traits an item carries, or null when it has none (how a fitted module stores them).</summary>
        public static List<ShipComponentTraitType> GetTraitsOrNull(uint item)
        {
            var traits = GetTraits(item);
            return traits.Count == 0 ? null : traits;
        }

        /// <summary>Stores the traits on the item and names them after its name.</summary>
        public static void SetTraits(uint item, IReadOnlyCollection<ShipComponentTraitType> traits)
        {
            var stored = ShipTraitRules.Format(traits);
            if (string.IsNullOrEmpty(stored))
            {
                DeleteLocalString(item, ShipTraitRules.TraitsVariable);
                SetName(item, string.Empty);
                return;
            }

            SetLocalString(item, ShipTraitRules.TraitsVariable, stored);
            SetName(item, ShipTraitRules.DecoratedName(GetName(item, true), traits));
        }

        /// <summary>Adds traits to the ones an item already carries (a blueprint's bonuses on a crafted module).</summary>
        public static void AddTraits(uint item, IReadOnlyCollection<ShipComponentTraitType> traits)
        {
            if (traits == null || traits.Count == 0)
                return;

            SetTraits(item, GetTraits(item).Concat(traits).ToList());
        }

        /// <summary>
        /// After a successful craft: a crafted variant stamps its trait on the component it made, and a ship module
        /// inherits the traits of the components it consumed.
        /// </summary>
        public static void ApplyCraftedTraits(RecipeType recipeType, RecipeDetail recipe, uint item, IEnumerable<string> serializedComponents)
        {
            var variant = ShipTraitRules.VariantTrait(recipeType);
            if (variant != ShipComponentTraitType.None)
            {
                SetTraits(item, new[] { variant });
                return;
            }

            if (!InheritsTraits(recipe))
                return;

            var inherited = new List<ShipComponentTraitType>();
            foreach (var serialized in serializedComponents)
            {
                var component = ObjectPlugin.Deserialize(serialized);
                if (!GetIsObjectValid(component))
                    continue;

                if (CarriesTraits(GetResRef(component)))
                    inherited.AddRange(GetTraits(component));

                DestroyObject(component);
            }

            if (inherited.Count > 0)
                SetTraits(item, inherited);
        }

        /// <summary>Whether items of the resref carry traits: the components and the capital parts made from them (W11o).</summary>
        public static bool CarriesTraits(string resref) =>
            ShipTraitRules.IsComponent(resref) || ShipRareComponentRules.IsCapitalPart(resref);

        /// <summary>
        /// Whether the recipe's item takes on its components' traits: the revamp ship modules and ship deeds, and the
        /// capital parts, which pass their tier 5 components' traits on to the Capital hull built from them.
        /// </summary>
        public static bool InheritsTraits(RecipeDetail recipe)
        {
            var inherits = recipe.Category == RecipeCategoryType.ShipModule ||
                           recipe.Category == RecipeCategoryType.Starship && IsHullDeed(recipe.Resref) ||
                           recipe.Category == RecipeCategoryType.CapitalConstruction && ShipRareComponentRules.IsCapitalPart(recipe.Resref);
            return inherits && recipe.Components.Keys.Any(CarriesTraits);
        }

        /// <summary>
        /// Orders a recipe's candidate components so the crafting window, which takes them from the end of the list,
        /// spends traited components on a module (which keeps them) and plain ones on everything else.
        /// </summary>
        public static List<uint> OrderForConsumption(RecipeDetail recipe, List<uint> components)
        {
            var inherits = InheritsTraits(recipe);
            return components
                .OrderBy(item => CarriesTraits(GetResRef(item)) && (GetTraits(item).Count > 0) == inherits ? 1 : 0)
                .ToList();
        }

        /// <summary>
        /// Whether a held item may fill an Advanced deed recipe's rare-trait slot or stand in for its rare component
        /// (W11o, Appendix D): a component of the hull's tier carrying a rare trait.
        /// </summary>
        public static bool IsStandInCandidate(RecipeDetail recipe, uint item)
        {
            return recipe.Category == RecipeCategoryType.Starship &&
                   ShipRareComponentRules.CanStandIn(recipe.Resref, GetResRef(item), GetTraits(item));
        }

        /// <summary>The held items that will fill the recipe's rare-trait slot or its missing rare components, one item each.</summary>
        public static List<uint> ReserveStandIns(RecipeDetail recipe, IReadOnlyList<uint> components)
        {
            if (recipe.Category != RecipeCategoryType.Starship)
                return new List<uint>();

            var held = components
                .Select(x => (GetResRef(x), GetItemStackSize(x), (IReadOnlyCollection<ShipComponentTraitType>)GetTraits(x)))
                .ToList();
            return ShipRareComponentRules.ReserveStandIns(recipe.Resref, recipe.Components, held)
                .Select(index => components[index])
                .ToList();
        }

        /// <summary>Adds a component's traits, or a ship deed's hull traits, to its examine text.</summary>
        [NWNEventHandler(ScriptName.OnExamineObjectBefore)]
        public static void ExamineComponent()
        {
            var item = StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID"));
            if (GetObjectType(item) != ObjectType.Item)
                return;

            var resref = GetResRef(item);
            if (CarriesTraits(resref))
                SetDescription(item, BuildDescription(GetDescription(item, true), GetTraits(item)));
            else if (IsHullDeed(resref))
                SetDescription(item, BuildHullDescription(GetDescription(item, true), GetTraits(item)));
        }

        /// <summary>A ship deed's description with one line per hull trait, giving what it does on the hull.</summary>
        public static string BuildHullDescription(string description, IEnumerable<ShipComponentTraitType> traits)
        {
            var lines = ShipTraitRules.DescribeHullLines(traits);
            if (lines.Count == 0)
                return description;

            var text = ColorToken.Green("Hull traits:") + "\n" + string.Join("\n", lines);
            return string.IsNullOrWhiteSpace(description) ? text : $"{description}\n\n{text}";
        }

        /// <summary>An item's description with one line per trait after it.</summary>
        public static string BuildDescription(string description, IEnumerable<ShipComponentTraitType> traits)
        {
            var lines = ShipTraitRules.DescribeLines(traits);
            if (lines.Count == 0)
                return description;

            var text = ColorToken.Green("Traits:") + "\n" + string.Join("\n", lines);
            return string.IsNullOrWhiteSpace(description) ? text : $"{description}\n\n{text}";
        }
    }
}
