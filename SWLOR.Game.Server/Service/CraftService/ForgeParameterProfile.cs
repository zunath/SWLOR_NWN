using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// Turns a Smithery recipe into Plasma Forge parameters. Metal goods work in a narrower range as their level rises and cool
    /// faster, and light goods (rings, necklaces) lose heat faster than heavy ones (breastplates, shields). Leather and synthweave
    /// goods have no refined metal, so they are heat-sealed: a low, wide range that cools slowly. Armor is mostly Shape, weapons and
    /// jewelry mostly Finish.
    /// <para>
    /// Recipes of one level band must not all want the same play, or one memorized sequence serves them all (the CraftSim "no single
    /// rotation" gate). So the profile spreads them: each recipe gets a deterministic offset of its range center, cooling rate and
    /// starting Thermal from its own name, by an amount the level curves below set, and the Shape share is spread twice as wide as the
    /// categories alone would put it. A recipe may start outside its working range.
    /// </para>
    /// </summary>
    public sealed class ForgeParameterProfile : RecipeParameterProfile<ForgeParameters>
    {
        private const string MetalPrefix = "ref_";
        private static readonly string[] HeatSealedPrefixes = { "lth_", "fiberp_" };

        // The generator's own per-parameter variation is off. The profile spreads recipes itself (see Jitter), by amounts that depend on
        // the level band, which the generator cannot express.
        private static readonly IReadOnlyList<RecipeParameterDefinition> _definitions = new[]
        {
            RecipeParameterDefinition.Integer("rangeCenter", "Forge Range Center", 20, 80),
            RecipeParameterDefinition.Integer("rangeWidth", "Forge Range Width", 8, 60),
            RecipeParameterDefinition.Integer("coolingRate", "Forge Cooling Rate", 1, 12),
            RecipeParameterDefinition.Integer("startingThermal", "Forge Starting Thermal", 0, 100),
            RecipeParameterDefinition.Integer("shapeShare", "Forge Shape Share", 25, 75),
        };

        // Each curve below holds one value per recipe-level band (1-10, 11-20, 21-30, 31-40, 41-53) and a last one for level 53: inside the
        // last band the value moves in a straight line from the level 47 value to the level 53 one, so the hardest recipes can be eased.
        // At the edge between two bands the last and first levels take a quarter of the neighbor band's value, so the step is not a cliff.

        /// <summary>The working range's width for metal goods.</summary>
        private static readonly double[] MetalWidthCurve = { 36, 42, 22, 24, 24, 24 };

        /// <summary>The cooling rate of a mid-weight good.</summary>
        private static readonly double[] CoolingCurve = { 0.5, 3, 3.5, 4.5, 4.26, 5.5 };

        /// <summary>How much faster the lightest goods cool than the heaviest, in Thermal points per action.</summary>
        private static readonly double[] CoolingMassSpreadCurve = { 5, 3, 4, 9, 8, 6 };

        /// <summary>Added to the Thermal a piece starts at.</summary>
        private static readonly double[] StartBiasCurve = { -8, 0, -8, 0, 0, 8 };

        /// <summary>How far, either way, a recipe's name moves its range width, cooling rate, range center and starting Thermal.</summary>
        private static readonly double[] WidthJitterCurve = { 4, 12, 4, 4, 0, 0 };
        private static readonly double[] CoolingJitterCurve = { 0, 4, 4, 0, 0, 1 };
        private static readonly double[] CenterJitterCurve = { 2, 6, 0, 4, 4, 4 };
        private static readonly double[] ShareJitterCurve = { 0, 2, 0, 0, 0, 0 };
        private static readonly double[] StartJitterCurve = { 0, 0, 16, 0, 8, 0 };

        /// <summary>The Shape share is spread this many times wider than the category table puts it.</summary>
        private const double ShareSpread = 2.0;

        /// <summary>Heat-sealed goods work in a range this much wider than metal at the same level, and cool this much slower.</summary>
        private const double HeatSealedExtraWidth = 16;
        private const double HeatSealedCoolingReduction = 1;

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
            var mass = Mass(recipe.Category);
            var metalWidth = Math.Round(Curve(MetalWidthCurve, level) + Jitter(recipe, "W", Curve(WidthJitterCurve, level)));
            var metalCooling = Math.Max(
                1,
                Math.Round(Curve(CoolingCurve, level) + (0.5 - mass) * Curve(CoolingMassSpreadCurve, level) + Jitter(recipe, "C", Curve(CoolingJitterCurve, level))));

            double center;
            double width;
            double cooling;
            if (IsHeatSealed(recipe.Components))
            {
                center = 36 + Math.Round(level / 50.0 * 6.0);
                width = Math.Min(60, metalWidth + HeatSealedExtraWidth);
                cooling = Math.Max(1, metalCooling - HeatSealedCoolingReduction);
            }
            else
            {
                center = MetalCenter(recipe.Category) + Jitter(recipe, "Ctr", Curve(CenterJitterCurve, level));
                width = metalWidth;
                cooling = metalCooling;
            }

            width = Math.Max(8, width);

            // The piece starts near the top of the range at easy levels and lower in it at hard ones, then the recipe's own offset moves it.
            var high = center - (int)width / 2 + width;
            var start = high - Math.Round(width * (0.10 + 0.40 * level / 50.0));
            start += Curve(StartBiasCurve, level) + Jitter(recipe, "S", Curve(StartJitterCurve, level));

            return new Dictionary<string, double>
            {
                ["rangeCenter"] = Math.Clamp(Math.Round(center), 20, 80),
                ["rangeWidth"] = Math.Clamp(width, 8, 60),
                ["coolingRate"] = Math.Clamp(cooling, 1, 12),
                ["startingThermal"] = Math.Clamp(Math.Round(start), 0, 100),
                ["shapeShare"] = Math.Clamp(Math.Round(50 + (ShapeShare(recipe.Category) - 50) * ShareSpread + Jitter(recipe, "Sh", Curve(ShareJitterCurve, level))), 25, 75),
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

        /// <summary>The difficulty each recipe-level band allows, as (lowest, highest): the band's own recipes with a margin of 6 either way.</summary>
        private static readonly (double Lowest, double Highest)[] BandBudgets =
        {
            (46, 95), (42, 121), (60, 143), (70, 134), (63, 130)
        };

        public override DifficultyBudget BudgetFor(int recipeLevel)
        {
            var (lowest, highest) = BandBudgets[BandOf(Math.Clamp(recipeLevel, 1, 52))];
            return new DifficultyBudget(lowest, highest);
        }

        /// <summary>
        /// A value that depends on the recipe level: the level's band value (the last band moves in a straight line from its level 47 value to
        /// its level 53 one), with a quarter of the neighboring band's value at the last and first level of a band.
        /// </summary>
        private static double Curve(double[] values, int level)
        {
            var own = BandValue(values, level);
            if (level < TopLevel && BandOf(level + 1) != BandOf(level))
                return 0.75 * own + 0.25 * BandValue(values, level + 1);

            if (level > 1 && BandOf(level - 1) != BandOf(level))
                return 0.75 * own + 0.25 * BandValue(values, level - 1);

            return own;
        }

        private const int TopLevel = 53;
        private const int LastBandRampFrom = 47;

        private static int BandOf(int level) => level <= 10 ? 0 : level <= 20 ? 1 : level <= 30 ? 2 : level <= 40 ? 3 : 4;

        private static double BandValue(double[] values, int level)
        {
            var band = BandOf(level);
            if (band < 4)
                return values[band];

            return level <= LastBandRampFrom
                ? values[4]
                : values[4] + (values[5] - values[4]) * (level - LastBandRampFrom) / (double)(TopLevel - LastBandRampFrom);
        }

        /// <summary>A repeatable offset between minus and plus <paramref name="amplitude"/>, drawn from the recipe's own name and the parameter.</summary>
        private static double Jitter(RecipeParameterContext recipe, string parameter, double amplitude)
        {
            if (amplitude <= 0)
                return 0;

            return (RecipeParameterSeed.Unit("Forge", recipe.Type.ToString(), "jit" + parameter, 0) * 2 - 1) * amplitude;
        }

        /// <summary>0 for the lightest goods (they lose heat fastest) to 1 for the heaviest.</summary>
        private static double Mass(RecipeCategoryType category)
        {
            switch (category)
            {
                case RecipeCategoryType.Breastplate:
                case RecipeCategoryType.Shield:
                    return 1.0;
                case RecipeCategoryType.GreatSword:
                case RecipeCategoryType.Saberstaff:
                case RecipeCategoryType.Rifle:
                    return 0.8;
                case RecipeCategoryType.Helmet:
                case RecipeCategoryType.Legging:
                case RecipeCategoryType.Tunic:
                case RecipeCategoryType.Boots:
                    return 0.6;
                case RecipeCategoryType.Longsword:
                case RecipeCategoryType.Spear:
                case RecipeCategoryType.Staff:
                case RecipeCategoryType.TwinBlade:
                    return 0.5;
                case RecipeCategoryType.Bracer:
                case RecipeCategoryType.Glove:
                case RecipeCategoryType.Cap:
                case RecipeCategoryType.PowerGlove:
                case RecipeCategoryType.Pistol:
                case RecipeCategoryType.TwinPistol:
                    return 0.35;
                case RecipeCategoryType.Belt:
                case RecipeCategoryType.Cloak:
                case RecipeCategoryType.Lightsaber:
                case RecipeCategoryType.Knife:
                case RecipeCategoryType.Katar:
                    return 0.25;
                case RecipeCategoryType.Ring:
                case RecipeCategoryType.Necklace:
                case RecipeCategoryType.Shuriken:
                    return 0.0;
                default:
                    return 0.5;
            }
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
