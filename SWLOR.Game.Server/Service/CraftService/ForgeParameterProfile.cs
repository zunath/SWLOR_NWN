using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// Turns a Smithery recipe into Plasma Forge parameters. Metal goods work in a narrower range as their level rises and cool
    /// faster. Leather and synthweave goods have no refined metal, so they are heat-sealed: a low, wide range that cools slowly.
    /// Armor is mostly Shape, weapons and jewelry mostly Finish.
    /// </summary>
    public sealed class ForgeParameterProfile : RecipeParameterProfile<ForgeParameters>
    {
        private const string MetalPrefix = "ref_";
        private static readonly string[] HeatSealedPrefixes = { "lth_", "fiberp_" };

        private static readonly IReadOnlyList<RecipeParameterDefinition> _definitions = new[]
        {
            RecipeParameterDefinition.Integer("rangeCenter", "Forge Range Center", 30, 70, 4),
            RecipeParameterDefinition.Integer("rangeWidth", "Forge Range Width", 14, 60, 4),
            RecipeParameterDefinition.Integer("coolingRate", "Forge Cooling Rate", 1, 8, 1),
            RecipeParameterDefinition.Integer("startingThermal", "Forge Starting Thermal", 10, 90, 4),
            RecipeParameterDefinition.Integer("shapeShare", "Forge Shape Share", 35, 65, 3),
        };

        private static readonly IReadOnlyDictionary<RecipeType, RecipeType> _derived = new Dictionary<RecipeType, RecipeType>
        {
            [RecipeType.BasicTwinPistol] = RecipeType.BasicPistol,
            [RecipeType.FieldTwinPistol] = RecipeType.FieldPistol,
            [RecipeType.TitanTwinPistol] = RecipeType.TitanPistol,
            [RecipeType.SithTwinPistol] = RecipeType.SithPistol,
            [RecipeType.VeteranTwinPistol] = RecipeType.VeteranPistol,
            [RecipeType.DeltaTwinPistol] = RecipeType.DeltaPistol,
            [RecipeType.PrimeTwinPistol] = RecipeType.PrimePistol,
            [RecipeType.ProtoTwinPistol] = RecipeType.ProtoPistol,
            [RecipeType.AscendantTwinPistol] = RecipeType.AscendantPistol,
            [RecipeType.OphidianTwinPistol] = RecipeType.OphidianPistol,
            [RecipeType.ChiroTwinPistol] = RecipeType.ChiroPistol,
        };

        public override string Key => "Forge";

        public override SkillType Skill => SkillType.Smithery;

        public override IReadOnlyList<RecipeParameterDefinition> Definitions => _definitions;

        public override IReadOnlyDictionary<RecipeType, RecipeType> DerivedRecipes => _derived;

        /// <summary>True when the recipe has no refined metal and is made of leather or synthweave.</summary>
        public static bool IsHeatSealed(IReadOnlyDictionary<string, int> components)
        {
            var resrefs = components.Keys.ToList();
            return resrefs.All(resref => !resref.StartsWith(MetalPrefix, StringComparison.Ordinal)) &&
                   resrefs.Any(resref => HeatSealedPrefixes.Any(prefix => resref.StartsWith(prefix, StringComparison.Ordinal)));
        }

        public override IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe)
        {
            var level = Math.Clamp(recipe.Level, 1, 52);
            var metalWidth = Math.Round(56 - (level - 1) * 0.65);
            var metalCooling = 2 + Math.Round((level - 1) * 4.0 / 49.0);
            var sealed_ = IsHeatSealed(recipe.Components);

            double center;
            double width;
            double cooling;
            if (sealed_)
            {
                center = 36 + Math.Round(level / 50.0 * 6.0);
                width = Math.Min(60, metalWidth + 16);
                cooling = Math.Max(1, metalCooling - 1);
            }
            else
            {
                center = MetalCenter(recipe.Category);
                width = metalWidth;
                cooling = metalCooling;
            }

            // The piece starts hot at easy levels and nearer the center of the range at hard ones.
            var high = center - (int)width / 2 + width;
            var start = high - Math.Round(width * (0.10 + 0.40 * level / 50.0));

            return new Dictionary<string, double>
            {
                ["rangeCenter"] = center,
                ["rangeWidth"] = width,
                ["coolingRate"] = cooling,
                ["startingThermal"] = start,
                ["shapeShare"] = ShapeShare(recipe.Category),
            };
        }

        protected override ForgeParameters Create(RecipeParameterValues values)
        {
            return new ForgeParameters(
                values.Int("rangeCenter"),
                values.Int("rangeWidth"),
                values.Int("coolingRate"),
                values.Int("startingThermal"),
                values.Int("shapeShare"));
        }

        /// <summary>
        /// A narrow range, fast cooling, a start lower in the range, and a start outside the range all make the forge harder.
        /// </summary>
        protected override double MeasureDifficulty(ForgeParameters parameters)
        {
            var belowTop = Math.Max(0, parameters.RangeHigh - parameters.StartingThermal);
            var outside = Math.Max(0, Math.Max(parameters.RangeLow - parameters.StartingThermal, parameters.StartingThermal - parameters.RangeHigh));
            return (100 - parameters.RangeWidth) + 5.0 * parameters.CoolingRate + 0.25 * belowTop + 3.0 * outside;
        }

        public override DifficultyBudget BudgetFor(int recipeLevel)
        {
            var level = Math.Clamp(recipeLevel, 1, 52);
            var metal = 55.0 + 1.10 * (level - 1);
            var heatSealed = 46.0 + 0.89 * (level - 1);
            return new DifficultyBudget(Math.Floor(heatSealed - 14.0), Math.Ceiling(metal + 14.0));
        }

        private static double MetalCenter(RecipeCategoryType category)
        {
            switch (category)
            {
                case RecipeCategoryType.Breastplate:
                case RecipeCategoryType.Shield:
                    return 64;
                case RecipeCategoryType.Helmet:
                case RecipeCategoryType.Bracer:
                case RecipeCategoryType.Legging:
                case RecipeCategoryType.Boots:
                case RecipeCategoryType.Glove:
                case RecipeCategoryType.Cap:
                case RecipeCategoryType.Tunic:
                    return 58;
                case RecipeCategoryType.Ring:
                case RecipeCategoryType.Necklace:
                case RecipeCategoryType.Belt:
                case RecipeCategoryType.Cloak:
                    return 48;
                case RecipeCategoryType.Pistol:
                case RecipeCategoryType.TwinPistol:
                case RecipeCategoryType.Rifle:
                    return 54;
                case RecipeCategoryType.Lightsaber:
                case RecipeCategoryType.Saberstaff:
                    return 52;
                default:
                    return 60;
            }
        }

        private static double ShapeShare(RecipeCategoryType category)
        {
            switch (category)
            {
                case RecipeCategoryType.Shield:
                    return 62;
                case RecipeCategoryType.Breastplate:
                case RecipeCategoryType.Tunic:
                case RecipeCategoryType.Legging:
                    return 60;
                case RecipeCategoryType.Helmet:
                case RecipeCategoryType.Bracer:
                case RecipeCategoryType.Boots:
                case RecipeCategoryType.Glove:
                case RecipeCategoryType.Cap:
                    return 56;
                case RecipeCategoryType.Belt:
                case RecipeCategoryType.Cloak:
                    return 52;
                case RecipeCategoryType.PowerGlove:
                    return 46;
                case RecipeCategoryType.Ring:
                case RecipeCategoryType.Necklace:
                    return 44;
                case RecipeCategoryType.Pistol:
                case RecipeCategoryType.TwinPistol:
                case RecipeCategoryType.Rifle:
                    return 40;
                case RecipeCategoryType.Lightsaber:
                case RecipeCategoryType.Saberstaff:
                    return 38;
                case RecipeCategoryType.Longsword:
                case RecipeCategoryType.GreatSword:
                case RecipeCategoryType.Knife:
                case RecipeCategoryType.Katar:
                case RecipeCategoryType.Spear:
                case RecipeCategoryType.Staff:
                case RecipeCategoryType.TwinBlade:
                case RecipeCategoryType.Shuriken:
                    return 42;
                default:
                    return 50;
            }
        }
    }
}
