using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// Turns an Agriculture recipe into Kitchen parameters. The dish comes from the recipe's category and name. The flavor target
    /// sits at a share of what the committed ingredients add up to, so the committed ingredients can reach it and a cook decides how
    /// much of them to use. The radii follow the dish and tighten with the recipe's level, and Freshness follows how many turns
    /// the recipe needs.
    /// </summary>
    public sealed class KitchenParameterProfile : RecipeParameterProfile<KitchenParameters>
    {
        /// <summary>The smallest inner radius a recipe may have.</summary>
        public const int MinInnerRadius = 4;

        /// <summary>The largest inner radius a recipe may have.</summary>
        public const int MaxInnerRadius = 14;

        public const int MinOuterRadius = 18;
        public const int MaxOuterRadius = 60;
        public const int MinFreshness = 50;
        public const int MaxFreshness = 150;

        /// <summary>An empty pot must score 0% quality, so the outer radius stays this far short of the center's distance from it.</summary>
        public const int EmptyPotMargin = 3;

        /// <summary>The nearest a dish's target center sits to an empty pot.</summary>
        public const int MinDistance = 24;

        /// <summary>The nearest a pet food's target center sits to an empty pot.</summary>
        public const int MinPetFoodDistance = 32;

        /// <summary>The Freshness a recipe gets beyond the turns it needs, as a percentage.</summary>
        private const int FreshnessSlackPercent = 105;

        /// <summary>Turns spent adjusting heat and correcting flavor on top of the adds and the simmering.</summary>
        private const int HandlingTurns = 4;

        private static readonly string[] SweetWords =
            { "Cookies", "Juice", "AuLait", "Cake", "FlapJack", "Fizz", "Gardula", "Noga", "Treat" };

        private static readonly string[] SoupWords =
            { "Soup", "Broth", "Stew", "Chowder", "Curry", "Noodles", "Ramen", "Gumbo", "Consomme", "Tea", "Bowl" };

        /// <summary>Dishes whose name points at the wrong dish: a travel cake is a savory ration, not a dessert.</summary>
        private static readonly IReadOnlyDictionary<RecipeType, KitchenDish> DishExceptions =
            new Dictionary<RecipeType, KitchenDish>
            {
                [RecipeType.DustveilTravelCakes] = KitchenDish.Roast
            };

        private static readonly IReadOnlyList<RecipeParameterDefinition> _definitions = new[]
        {
            RecipeParameterDefinition.Enumeration<KitchenDish>("dish", "Kitchen Dish"),
            RecipeParameterDefinition.Integer("targetSavory", "Kitchen Target Savory", 0, KitchenTuning.AxisMax, 6),
            RecipeParameterDefinition.Integer("targetSweet", "Kitchen Target Sweet", 0, KitchenTuning.AxisMax, 6),
            RecipeParameterDefinition.Integer("targetSpice", "Kitchen Target Spice", 0, KitchenTuning.AxisMax, 6),
            RecipeParameterDefinition.Integer("innerRadius", "Kitchen Inner Radius", MinInnerRadius, MaxInnerRadius, 2),
            RecipeParameterDefinition.Integer("outerRadius", "Kitchen Outer Radius", MinOuterRadius, MaxOuterRadius, 4),
            RecipeParameterDefinition.Integer("freshness", "Kitchen Freshness", MinFreshness, MaxFreshness, 4)
        };

        public override string Key => "Kitchen";

        public override SkillType Skill => SkillType.Agriculture;

        public override IReadOnlyList<RecipeParameterDefinition> Definitions => _definitions;

        public override IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe)
        {
            var dish = Classify(recipe);
            var center = TargetCenter(dish, recipe);
            var (inner, outer) = Radii(dish, recipe.Level, center);

            return new Dictionary<string, double>
            {
                ["dish"] = (int)dish,
                ["targetSavory"] = center.Savory,
                ["targetSweet"] = center.Sweet,
                ["targetSpice"] = center.Spice,
                ["innerRadius"] = inner,
                ["outerRadius"] = outer,
                ["freshness"] = Freshness(recipe, center)
            };
        }

        protected override KitchenParameters Create(RecipeParameterValues values)
        {
            return new KitchenParameters(
                values.EnumValue<KitchenDish>("dish"),
                values.Int("targetSavory"),
                values.Int("targetSweet"),
                values.Int("targetSpice"),
                values.Int("innerRadius"),
                values.Int("outerRadius"),
                values.Int("freshness"));
        }

        /// <summary>
        /// How hard the parameters make the dish, 0 to 100: a small perfect zone, a steep fall to 0%, and little Freshness all make it
        /// harder. A target an empty pot already scores on is rejected outright, because a cook could then do nothing and still earn
        /// quality.
        /// </summary>
        protected override double MeasureDifficulty(KitchenParameters parameters)
        {
            var center = new FlavorVector(parameters.TargetSavory, parameters.TargetSweet, parameters.TargetSpice);
            if (parameters.OuterRadius >= center.Length)
                return RejectedDifficulty;

            var tightness = (MaxInnerRadius - parameters.InnerRadius) / (double)(MaxInnerRadius - MinInnerRadius) * 40;
            var steepness = (MaxOuterRadius - (parameters.OuterRadius - parameters.InnerRadius))
                / (double)(MaxOuterRadius - MinInnerRadius) * 40;
            var scarcity = (MaxFreshness - parameters.Freshness) / (double)(MaxFreshness - MinFreshness) * 20;

            return tightness + steepness + scarcity;
        }

        /// <summary>Above every budget, so a draw whose target an empty pot reaches is never kept.</summary>
        public const double RejectedDifficulty = 1000;

        /// <summary>Difficulty runs from about 55 for a level 1 dish to about 80 for a level 50 dish, and may stray 25 either way.</summary>
        public override DifficultyBudget BudgetFor(int recipeLevel)
        {
            var expected = 55 + recipeLevel * 0.5;
            return new DifficultyBudget(expected - 25, expected + 25);
        }

        /// <summary>The dish a recipe cooks.</summary>
        public static KitchenDish Classify(RecipeParameterContext recipe)
        {
            if (recipe.Category == RecipeCategoryType.PetFood)
                return KitchenDish.PetFood;
            if (recipe.Category == RecipeCategoryType.CookingEnhancement)
                return KitchenDish.Soup;

            if (DishExceptions.TryGetValue(recipe.Type, out var exception))
                return exception;

            var name = recipe.Type.ToString();
            if (SweetWords.Any(word => name.Contains(word, StringComparison.Ordinal)))
                return KitchenDish.Sweet;
            if (SoupWords.Any(word => name.Contains(word, StringComparison.Ordinal)))
                return KitchenDish.Soup;

            return KitchenDish.Roast;
        }

        /// <summary>What every committed unit adds up to, ignoring ingredients that only thin the pot.</summary>
        public static FlavorVector CommittedFlavor(IReadOnlyDictionary<string, int> components)
        {
            var total = FlavorVector.Zero;
            foreach (var component in components)
            {
                var flavor = KitchenComponentTable.Get(component.Key);
                if (flavor.Kind == KitchenIngredientKind.Flavor)
                    total += flavor.Vector.ScaledBy(component.Value, 1);
            }

            return total;
        }

        /// <summary>The portions the recipe's ingredients are added in.</summary>
        public static int Portions(IReadOnlyDictionary<string, int> components)
        {
            return components.Sum(component => Math.Min(component.Value, KitchenTuning.MaxPortionsPerLine));
        }

        private static FlavorVector TargetCenter(KitchenDish dish, RecipeParameterContext recipe)
        {
            var share = dish switch
            {
                KitchenDish.Soup => 75,
                KitchenDish.Roast => 80,
                KitchenDish.Sweet => 80,
                _ => 90
            };

            var center = CommittedFlavor(recipe.Components).Scaled(share).Clamped(KitchenTuning.AxisMax);

            // A recipe of mild ingredients still asks for a target the pot starts well away from; the pantry makes up the difference.
            var minimum = dish == KitchenDish.PetFood ? MinPetFoodDistance : MinDistance;
            var length = center.Length;
            if (length < minimum)
            {
                var factor = length < 1 ? 0 : minimum / length;
                center = length < 1
                    ? new FlavorVector(minimum, 0, 0)
                    : new FlavorVector(
                        (int)Math.Ceiling(center.Savory * factor),
                        (int)Math.Ceiling(center.Sweet * factor),
                        (int)Math.Ceiling(center.Spice * factor)).Clamped(KitchenTuning.AxisMax);
            }

            return center;
        }

        private static (int Inner, int Outer) Radii(KitchenDish dish, int level, FlavorVector center)
        {
            var (innerStart, outerStart) = dish switch
            {
                KitchenDish.Soup => (12.0, 38.0),
                KitchenDish.Roast => (10.0, 34.0),
                KitchenDish.Sweet => (9.0, 30.0),
                _ => (10.0, 28.0)
            };

            var inner = Math.Clamp((int)Math.Round(innerStart - level * 0.12), MinInnerRadius, MaxInnerRadius);
            var outer = Math.Clamp((int)Math.Round(outerStart - level * 0.3), MinOuterRadius, MaxOuterRadius);

            // An empty pot must not score, so the zone cannot reach back to the origin.
            var reach = (int)Math.Floor(center.Length) - EmptyPotMargin;
            outer = Math.Max(MinOuterRadius, Math.Min(outer, reach));
            inner = Math.Min(inner, outer - 4);
            return (Math.Max(MinInnerRadius, inner), outer);
        }

        private static int Freshness(RecipeParameterContext recipe, FlavorVector center)
        {
            // Any distance the committed ingredients cannot cover is made up a pinch at a time from the pantry.
            var shortfall = Math.Max(0, center.Length - CommittedFlavor(recipe.Components).Length);
            var pantryTurns = (int)Math.Ceiling(shortfall / KitchenTuning.PantryPush);

            var turns = Portions(recipe.Components) + pantryTurns + HandlingTurns +
                        (int)Math.Ceiling(KitchenTuning.DonenessMax / (double)KitchenTuning.SimmerMediumDoneness);
            var freshness = turns * KitchenTuning.FreshnessDecayPerTurn * FreshnessSlackPercent / 100.0;
            return Math.Clamp((int)Math.Round(freshness), MinFreshness, MaxFreshness);
        }
    }
}
