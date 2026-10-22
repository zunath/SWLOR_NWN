using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Progression;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Support
{
    /// <summary>Hand-made craft results and recipes, and a model over the real recipes, for the progression tests.</summary>
    public static class ProgressionFixture
    {
        public const string Gear = CraftingGearCatalog.MidGearName;

        private static readonly Lazy<ComponentSupply> RealSupply = new(() => ComponentSupply.Build(SharedData.Recipes));
        private static readonly Lazy<GuildTaskCatalog> RealGuilds = new(() => GuildTaskCatalog.Build(SharedData.Recipes, SharedData.Root));
        private static readonly Lazy<ProgressionModel> RealModel = new(() => Model(SharedData.Recipes, RealSupply.Value, RealGuilds.Value, 0.95, 0.30, 15.0));

        public static ComponentSupply Supply => RealSupply.Value;
        public static GuildTaskCatalog Guilds => RealGuilds.Value;

        /// <summary>The four crafting skills over the real recipes with the same hand-made craft results at every level.</summary>
        public static ProgressionModel RealRecipes => RealModel.Value;

        /// <summary>A craft cell with exact results: <paramref name="success"/> of 100 runs succeed at <paramref name="quality"/>, with <paramref name="actions"/> actions per run.</summary>
        public static CraftCell Cell(SkillType skill, int level, double success, double quality, double actions, string gear = Gear) => new()
        {
            Skill = skill,
            RecipeLevel = level,
            Rank = Math.Min(level, 50),
            Gear = gear,
            Policy = "Lookahead",
            Runs = 100,
            Successes = (int)Math.Round(success * 100),
            QualitySum = Math.Round(success * 100) * quality,
            Actions = (long)Math.Round(actions * 100)
        };

        /// <summary>Cells at all three gear levels: the other two play at half and one and a half times the mid gear quality.</summary>
        public static IEnumerable<CraftCell> Cells(SkillType skill, double success, double quality, double actions) =>
            Enumerable.Range(1, 53).SelectMany(level => new[]
            {
                Cell(skill, level, success, quality * 0.5, actions, CraftingGearCatalog.NoGearName),
                Cell(skill, level, success, quality, actions),
                Cell(skill, level, success, Math.Min(1.0, quality * 1.5), actions, CraftingGearCatalog.BestGearName)
            });

        public static ProgressionModel Model(
            RecipeCatalog recipes, ComponentSupply supply, GuildTaskCatalog guilds, double success, double quality, double actions,
            IReadOnlyList<SkillType> skills = null, Func<SkillType, string, int, int, CraftCell> playOffLevel = null)
        {
            skills ??= CraftingGearCatalog.GearSkills;
            return ProgressionModel.FromCells(
                recipes,
                supply,
                guilds,
                new ProgressionOptions { Skills = skills, Runs = 100, Seed = 1 },
                skills.SelectMany(skill => Cells(skill, success, quality, actions)),
                playOffLevel);
        }

        /// <summary>One recipe per level 1-53 for a skill, each committing the given components.</summary>
        public static RecipeCatalog Recipes(SkillType skill, IReadOnlyDictionary<string, int> components, int recipesPerLevel = 1) =>
            RecipeCatalog.From(
                Enumerable.Range(1, 53).SelectMany(level => Enumerable.Range(0, recipesPerLevel).Select(copy =>
                    new RecipeEntry(default(RecipeType), skill, level, $"item_{level}_{copy}", default(RecipeCategoryType), components))));

        public static ProgressionModel HandMade(
            SkillType skill, IReadOnlyDictionary<string, int> components, double success, double quality, double actions, int recipesPerLevel = 1)
        {
            var recipes = Recipes(skill, components, recipesPerLevel);
            return Model(recipes, ComponentSupply.Build(recipes), null, success, quality, actions, new[] { skill });
        }
    }
}
