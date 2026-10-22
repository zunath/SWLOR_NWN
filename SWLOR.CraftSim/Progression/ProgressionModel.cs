using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>What a progression run plays, besides the timings in <see cref="ProgressionAssumptions"/>.</summary>
    public sealed class ProgressionOptions
    {
        /// <summary>Crafts played per recipe level, skill and gear level to find the success rate, quality and actions of the lookahead player.</summary>
        public int Runs { get; init; } = 500;

        public int Seed { get; init; } = 1;

        public IReadOnlyList<SkillType> Skills { get; init; } = CraftingGearCatalog.GearSkills;

        /// <summary>Lookahead futures sampled per action; the legacy baseline's default.</summary>
        public int Rollouts { get; init; } = RolloutPolicy.DefaultRollouts;
    }

    /// <summary>
    /// The inputs of the progression-pace baseline: the lookahead player's results at every recipe level (played from the seed), the
    /// recipes, the gear, the component supply and the guild tasks. Climbs under different timings and guild plans are computed from them.
    /// </summary>
    public sealed class ProgressionModel
    {
        private readonly Dictionary<(SkillType Skill, string Gear), Dictionary<int, CraftCell>> _cells;
        private readonly Dictionary<SkillType, IReadOnlyList<string>> _gearNames;

        private ProgressionModel(
            RecipeCatalog recipes,
            ComponentSupply supply,
            GuildTaskCatalog guilds,
            ProgressionOptions options,
            Dictionary<(SkillType, string), Dictionary<int, CraftCell>> cells,
            Dictionary<SkillType, IReadOnlyList<string>> gearNames)
        {
            Recipes = recipes;
            Supply = supply;
            Guilds = guilds;
            Options = options;
            _cells = cells;
            _gearNames = gearNames;
        }

        public RecipeCatalog Recipes { get; }
        public ComponentSupply Supply { get; }
        public GuildTaskCatalog Guilds { get; }
        public ProgressionOptions Options { get; }
        public IReadOnlyList<SkillType> Skills => Options.Skills;

        /// <summary>The gear levels played for a skill: no gear, mid gear, best gear.</summary>
        public IReadOnlyList<string> GearNames(SkillType skill) => _gearNames[skill];

        public static ProgressionModel Run(RecipeCatalog recipes, CraftingGearCatalog gear, GuildTaskCatalog guilds, ProgressionOptions options)
        {
            var baseline = new LegacyCraftBaselineOptions
            {
                Runs = options.Runs,
                Seed = options.Seed,
                Policies = new ILegacyCraftPolicy[] { new RolloutPolicy(options.Rollouts) },
                SensitivityOffsets = Array.Empty<int>(),
                Skills = options.Skills
            };

            var cells = LegacyCraftBaseline.Simulate(recipes, gear, baseline)
                .Where(c => c.RankOffset == 0)
                .GroupBy(c => (c.Skill, c.Gear))
                .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.RecipeLevel));

            var gearNames = options.Skills.ToDictionary(skill => skill, skill => (IReadOnlyList<string>)gear.Levels(skill).Select(l => l.Name).ToList());
            return new ProgressionModel(recipes, ComponentSupply.Build(recipes), guilds, options, cells, gearNames);
        }

        /// <summary>Builds a model from craft results played elsewhere, such as a test's hand-made ones.</summary>
        public static ProgressionModel FromCells(
            RecipeCatalog recipes,
            ComponentSupply supply,
            GuildTaskCatalog guilds,
            ProgressionOptions options,
            IEnumerable<CraftCell> cells)
        {
            var byKey = cells
                .GroupBy(c => (c.Skill, c.Gear))
                .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.RecipeLevel));
            var gearNames = options.Skills.ToDictionary(
                skill => skill,
                skill => (IReadOnlyList<string>)byKey.Keys.Where(k => k.Skill == skill).Select(k => k.Gear).OrderBy(n => n, StringComparer.Ordinal).ToList());
            return new ProgressionModel(recipes, supply, guilds, options, byKey, gearNames);
        }

        /// <summary>The crafter's results at one recipe level, from the played crafts.</summary>
        public CraftCell Cell(SkillType skill, string gear, int level) => _cells[(skill, gear)][level];

        public SkillPath Path(SkillType skill, string gear, ProgressionAssumptions assumptions, GuildHandInPlan plan = null) =>
            CraftingProgression.Simulate(skill, gear, _cells[(skill, gear)], Recipes, Supply, assumptions, plan);

        public GatheringPath Gathering(GatheringStrategy strategy, ProgressionAssumptions assumptions) =>
            GatheringProgression.Simulate(strategy, Supply, assumptions);

        /// <summary>Hours of a path's gathering, by the kind of node the components come from.</summary>
        public IReadOnlyDictionary<string, double> GatherHoursByFamily(SkillPath path, ProgressionAssumptions assumptions)
        {
            var hours = new Dictionary<string, double>();
            foreach (var step in path.Steps)
            {
                var consumed = step.SuccessRate + Legacy.Crafting.LegacyCraftRules.FailureLossPercent / 100.0 * (1 - step.SuccessRate);
                foreach (var (family, seconds) in Demand(path.Skill, step.RecipeLevel, assumptions).SecondsByFamily)
                    hours[family] = hours.GetValueOrDefault(family) + step.Crafts * consumed * seconds / 3600;
            }

            return hours;
        }

        /// <summary>Hours of a path's gathering, by component.</summary>
        public IReadOnlyDictionary<string, double> GatherHoursByComponent(SkillPath path, ProgressionAssumptions assumptions)
        {
            var hours = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var step in path.Steps)
            {
                var consumed = step.SuccessRate + Legacy.Crafting.LegacyCraftRules.FailureLossPercent / 100.0 * (1 - step.SuccessRate);
                foreach (var (component, seconds) in Demand(path.Skill, step.RecipeLevel, assumptions).SecondsByComponent)
                    hours[component] = hours.GetValueOrDefault(component) + step.Crafts * consumed * seconds / 3600;
            }

            return hours;
        }

        /// <summary>The components one craft at a recipe level commits, averaged over the level's recipes.</summary>
        public LevelDemand Demand(SkillType skill, int level, ProgressionAssumptions assumptions) =>
            LevelDemand.Build(Recipes.ForSkill(skill).Where(r => r.Level == level).ToList(), Supply, assumptions);
    }
}
