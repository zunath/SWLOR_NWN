using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Ship component traits on items (plan section 14). A trait is stored as an item local
    /// (<see cref="ShipTraitRules.TraitsVariable"/>) and named in brackets after the item's name. Crafted variants stamp
    /// their trait on the component; a revamp module crafted from components inherits every trait they carried.
    /// </summary>
    public static class ShipComponents
    {
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

                if (ShipTraitRules.IsComponent(GetResRef(component)))
                    inherited.AddRange(GetTraits(component));

                DestroyObject(component);
            }

            if (inherited.Count > 0)
                SetTraits(item, inherited);
        }

        /// <summary>Whether the recipe's item takes on its components' traits: the revamp ship modules.</summary>
        public static bool InheritsTraits(RecipeDetail recipe)
        {
            return recipe.Category == RecipeCategoryType.ShipModule &&
                   recipe.Components.Keys.Any(ShipTraitRules.IsComponent);
        }

        /// <summary>
        /// Orders a recipe's candidate components so the crafting window, which takes them from the end of the list,
        /// spends traited components on a module (which keeps them) and plain ones on everything else.
        /// </summary>
        public static List<uint> OrderForConsumption(RecipeDetail recipe, List<uint> components)
        {
            var inherits = InheritsTraits(recipe);
            return components
                .OrderBy(item => ShipTraitRules.IsComponent(GetResRef(item)) && (GetTraits(item).Count > 0) == inherits ? 1 : 0)
                .ToList();
        }

        /// <summary>Adds a component's traits to its examine text.</summary>
        [NWNEventHandler(ScriptName.OnExamineObjectBefore)]
        public static void ExamineComponent()
        {
            var item = StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID"));
            if (GetObjectType(item) != ObjectType.Item || !ShipTraitRules.IsComponent(GetResRef(item)))
                return;

            SetDescription(item, BuildDescription(GetDescription(item, true), GetTraits(item)));
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
