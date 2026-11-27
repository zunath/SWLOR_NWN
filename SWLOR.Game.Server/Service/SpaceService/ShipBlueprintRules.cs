using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Research blueprints for ship module and ship deed recipes (plan section 14) as pure functions. A blueprint's item
    /// bonuses and guaranteed bonuses are blueprint traits (<see cref="ShipTraitKind.Blueprint"/>), stamped on the
    /// crafted module or deed like component traits, so they reach the module's numbers through
    /// <see cref="ShipTraitRules"/> and the ship's through its hull traits.
    /// </summary>
    public static class ShipBlueprintRules
    {
        /// <summary>The blueprint item local that stores its guaranteed traits as comma-separated numbers.</summary>
        public const string GuaranteedTraitsVariable = "BLUEPRINT_SHIP_TRAITS";

        /// <summary>The blueprint item local set once a research job on it has spent Prototype Tech.</summary>
        public const string PrototypeTechVariable = "BLUEPRINT_PROTOTYPE_TECH";

        // The research ceiling (review 2026-10-30): space research is a bounded extension of ground research. A ground
        // blueprint gains at most 3 item bonuses (research levels 1, 6 and 9) and at most 11 guaranteed bonuses (level
        // 7's, plus one chance roll at each of the ten levels: 0.2% at level 1 up to 12.1% at level 10). A ship blueprint
        // keeps that ceiling: Prototype Tech adds its guaranteed trait at most once per blueprint and counts inside the 11,
        // every trait's tier is capped by the recipe level (BonusTier), and no trait repeats on one blueprint.

        /// <summary>The most item bonuses research grants a blueprint (levels 1, 6 and 9), as on the ground.</summary>
        public const int MaximumItemBonuses = 3;

        /// <summary>The most guaranteed bonuses a ground blueprint can gain (level 7, plus one chance roll per level).</summary>
        public const int MaximumGuaranteedTraits = 11;

        /// <summary>The most blueprint traits one crafted ship module or deed carries from its blueprint.</summary>
        public const int MaximumTraits = MaximumItemBonuses + MaximumGuaranteedTraits;

        private static readonly Dictionary<string, ShipModuleStep> _stepsByTag =
            RevampModuleCatalog.Steps.ToDictionary(x => x.Tag);

        private static readonly HashSet<string> _hullTags = RevampShipCatalog.Hulls.Select(x => x.Tag).ToHashSet();

        /// <summary>Whether a recipe is a ship module or ship deed recipe the Research Terminal can turn into a blueprint.</summary>
        public static bool IsResearchable(RecipeDetail recipe) => IsModuleRecipe(recipe) || IsHullRecipe(recipe);

        /// <summary>Whether a recipe is a revamp ship module recipe.</summary>
        public static bool IsModuleRecipe(RecipeDetail recipe) =>
            recipe != null &&
            recipe.EnhancementType == RecipeEnhancementType.Module &&
            recipe.Resref != null &&
            _stepsByTag.ContainsKey(recipe.Resref);

        /// <summary>Whether a recipe is a revamp ship deed recipe.</summary>
        public static bool IsHullRecipe(RecipeDetail recipe) =>
            recipe != null &&
            recipe.EnhancementType == RecipeEnhancementType.Starship &&
            recipe.Resref != null &&
            _hullTags.Contains(recipe.Resref);

        /// <summary>
        /// The component family whose blueprint traits a module recipe rolls: the first generic component of its bill.
        /// Invalid for a recipe that is not a ship module recipe.
        /// </summary>
        public static ShipComponentFamily ComponentFamily(RecipeDetail recipe)
        {
            if (!IsModuleRecipe(recipe))
                return ShipComponentFamily.Invalid;

            foreach (var (resref, _) in _stepsByTag[recipe.Resref].Components)
            {
                var family = ShipTraitRules.FamilyOf(resref);
                if (family != ShipComponentFamily.Invalid && !RevampComponentCatalog.RareFamilies.Contains(family))
                    return family;
            }

            return ShipComponentFamily.Invalid;
        }

        /// <summary>
        /// The tier an item bonus rolls, as on the ground: the nth bonus is tier n, capped at 1 for recipes of level 20
        /// or less and at 2 for levels 21-40.
        /// </summary>
        public static int BonusTier(int recipeLevel, int bonusNumber)
        {
            var tier = Math.Clamp(bonusNumber, 1, 3);
            if (recipeLevel <= 20)
                return 1;
            if (recipeLevel <= 40)
                return Math.Min(tier, 2);
            return tier;
        }

        /// <summary>The blueprint traits of one tier a component family's modules can roll.</summary>
        public static List<ShipComponentTrait> Pool(ShipComponentFamily family, int tier) =>
            ShipTraitRules.Pool(family, ShipTraitKind.Blueprint)
                .Where(x => x.Tier == Math.Clamp(tier, 1, 3))
                .ToList();

        /// <summary>
        /// The blueprint traits of one tier a researched recipe rolls: a module's from its component family, a ship
        /// deed's the hull-side stats (<see cref="RevampComponentCatalog.HullBlueprintStats"/>). Empty for anything else.
        /// </summary>
        public static List<ShipComponentTrait> Pool(RecipeDetail recipe, int tier)
        {
            if (IsModuleRecipe(recipe))
                return Pool(ComponentFamily(recipe), tier);

            if (!IsHullRecipe(recipe))
                return new List<ShipComponentTrait>();

            return RevampComponentCatalog.Traits
                .Where(x => x.Kind == ShipTraitKind.Blueprint && x.Tier == Math.Clamp(tier, 1, 3) &&
                            x.Effects.Count == 1 && RevampComponentCatalog.HullBlueprintStats.Contains(x.Effects[0].Stat))
                .ToList();
        }

        /// <summary>Rolls a blueprint trait of the tier for a researched recipe. None when it has no pool.</summary>
        public static ShipComponentTraitType Pick(RecipeDetail recipe, int tier, Func<int, int> next)
        {
            var pool = Pool(recipe, tier);
            return pool.Count == 0 ? ShipComponentTraitType.None : pool[next(pool.Count)].Type;
        }

        /// <summary>
        /// A guaranteed trait for a researched recipe's blueprint: of the tier asked for, capped by the recipe's level
        /// (<see cref="BonusTier"/>), never one the blueprint holds. None once the blueprint holds
        /// <see cref="MaximumGuaranteedTraits"/> or every trait of that tier.
        /// </summary>
        public static ShipComponentTraitType PickGuaranteed(
            RecipeDetail recipe,
            int tier,
            IReadOnlyCollection<ShipComponentTraitType> held,
            Func<int, int> next)
        {
            if (recipe == null || held != null && held.Count >= MaximumGuaranteedTraits)
                return ShipComponentTraitType.None;

            var pool = Pool(recipe, BonusTier(recipe.Level, tier))
                .Where(x => held == null || !held.Contains(x.Type))
                .ToList();
            return pool.Count == 0 ? ShipComponentTraitType.None : pool[next(pool.Count)].Type;
        }

        /// <summary>
        /// The item bonuses a crafted module or deed rolls from its blueprint: the nth of at most
        /// <see cref="MaximumItemBonuses"/> rolls tier n capped by the recipe level, and none repeats another or one of the
        /// blueprint's guaranteed traits (a bonus whose tier has nothing left is skipped).
        /// </summary>
        public static List<ShipComponentTraitType> PickItemBonuses(
            RecipeDetail recipe,
            int itemBonuses,
            IReadOnlyCollection<ShipComponentTraitType> guaranteed,
            Func<int, int> next)
        {
            var picked = new List<ShipComponentTraitType>();
            if (recipe == null)
                return picked;

            for (var bonus = 1; bonus <= Math.Min(itemBonuses, MaximumItemBonuses); bonus++)
            {
                var pool = Pool(recipe, BonusTier(recipe.Level, bonus))
                    .Where(x => (guaranteed == null || !guaranteed.Contains(x.Type)) && !picked.Contains(x.Type))
                    .ToList();
                if (pool.Count > 0)
                    picked.Add(pool[next(pool.Count)].Type);
            }

            return picked;
        }

        /// <summary>
        /// Rolls a blueprint trait of the tier for a component family's module. <paramref name="next"/> returns a number
        /// from 0 to its argument minus 1. None when the family has no blueprint traits.
        /// </summary>
        public static ShipComponentTraitType Pick(ShipComponentFamily family, int tier, Func<int, int> next)
        {
            var pool = Pool(family, tier);
            return pool.Count == 0 ? ShipComponentTraitType.None : pool[next(pool.Count)].Type;
        }

        /// <summary>A guaranteed trait of the tier for a component family's module that the blueprint does not hold, or None.</summary>
        public static ShipComponentTraitType PickGuaranteed(
            ShipComponentFamily family,
            int tier,
            IReadOnlyCollection<ShipComponentTraitType> held,
            Func<int, int> next)
        {
            if (held != null && held.Count >= MaximumGuaranteedTraits)
                return ShipComponentTraitType.None;

            var pool = Pool(family, tier).Where(x => held == null || !held.Contains(x.Type)).ToList();
            return pool.Count == 0 ? ShipComponentTraitType.None : pool[next(pool.Count)].Type;
        }
    }
}
