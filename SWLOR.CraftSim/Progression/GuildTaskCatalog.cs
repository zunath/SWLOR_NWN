using System.Reflection;
using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>How a crafting-guild task's item is obtained.</summary>
    public enum GuildTaskKind
    {
        /// <summary>A player-crafted item, made by a recipe of the guild's skill.</summary>
        Crafted,

        /// <summary>Raw fish, caught with a rod (Agriculture's "Catch" tasks, such as <c>agr_tsk_017</c>).</summary>
        Caught,

        /// <summary>An item no recipe of the guild's skill makes and no fish matches.</summary>
        Other
    }

    /// <summary>One crafting-guild task: hand in items for credits and guild points.</summary>
    /// <param name="Tier">The guild rank that unlocks the task, 0 to 4.</param>
    /// <param name="Quantity">Items the task asks for.</param>
    /// <param name="Level">
    /// The level that sets the task's XP: the lowest level of a recipe that makes the item for a crafted task, the fish's level
    /// (<c>FishType</c>) for a caught one, and null for any other.
    /// </param>
    /// <param name="Recipe">The recipe that makes the item, for a crafted task.</param>
    public sealed record GuildTask(
        GuildType Guild,
        SkillType Skill,
        string QuestId,
        int Tier,
        string Resref,
        int Quantity,
        GuildTaskKind Kind,
        int? Level,
        RecipeEntry Recipe,
        int Credits,
        int GuildPoints);

    /// <summary>The Smithery, Engineering, Fabrication and Agriculture guild tasks, read from the server's quest definitions.</summary>
    public sealed class GuildTaskCatalog
    {
        private static readonly IReadOnlyList<(GuildType Guild, SkillType Skill, IQuestListDefinition Definition)> Guilds =
            new (GuildType Guild, SkillType Skill, IQuestListDefinition Definition)[]
        {
            (GuildType.SmitheryGuild, SkillType.Smithery, new SmitheryGuildQuestDefinition()),
            (GuildType.EngineeringGuild, SkillType.Engineering, new EngineeringGuildQuestDefinition()),
            (GuildType.FabricationGuild, SkillType.Fabrication, new FabricationGuildQuestDefinition()),
            (GuildType.AgricultureGuild, SkillType.Agriculture, new AgricultureGuildQuestDefinition())
        };

        private readonly List<GuildTask> _tasks = new();

        private GuildTaskCatalog()
        {
        }

        public IReadOnlyList<GuildTask> Tasks => _tasks;

        public static SkillType SkillOf(GuildType guild) => Guilds.Single(g => g.Guild == guild).Skill;

        public static GuildType GuildOf(SkillType skill) => Guilds.SingleOrDefault(g => g.Skill == skill).Guild;

        public static GuildTaskCatalog Build(RecipeCatalog recipes, string repositoryRoot)
        {
            var catalog = new GuildTaskCatalog();
            Guild.LoadData();

            // The task definitions look up each item's name while they build. The game fills that cache at boot by spawning
            // every blueprint; here every blueprint file name stands in so no engine call is needed. The cache is put back after.
            var cache = typeof(Cache).GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidDataException("Cache no longer has an ItemNamesByResref property; update GuildTaskCatalog.");
            var original = cache.GetValue(null);
            try
            {
                var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in Directory.EnumerateFiles(RepositoryPaths.ModuleItems(repositoryRoot), "*.uti.json"))
                    names[Path.GetFileName(file)[..^".uti.json".Length]] = "item";
                foreach (var recipe in recipes.Recipes.Where(r => !string.IsNullOrEmpty(r.Resref)))
                    names.TryAdd(recipe.Resref, "item");

                cache.SetValue(null, names);
                foreach (var (guild, skill, definition) in Guilds)
                    catalog.AddTasks(guild, skill, definition.BuildQuests().Values, recipes);
            }
            finally
            {
                cache.SetValue(null, original);
            }

            return catalog;
        }

        public IEnumerable<GuildTask> For(GuildType guild) => _tasks.Where(t => t.Guild == guild);

        /// <summary>The credits and guild points one task of the tier pays; every task of a tier pays the same.</summary>
        public (int Credits, int GuildPoints) RewardOfTier(GuildType guild, int tier)
        {
            var rewards = For(guild).Where(t => t.Tier == tier).Select(t => (t.Credits, t.GuildPoints)).Distinct().ToList();
            return rewards.Count == 1
                ? rewards[0]
                : throw new InvalidDataException($"{guild} tier {tier} pays {rewards.Count} different rewards, not one.");
        }

        /// <summary>For each recipe level, the lowest guild rank that unlocks a task for a recipe of that level.</summary>
        public IReadOnlyDictionary<int, int> TierByRecipeLevel(GuildType guild) =>
            For(guild)
                .Where(t => t.Kind == GuildTaskKind.Crafted)
                .GroupBy(t => t.Level.Value)
                .ToDictionary(g => g.Key, g => g.Min(t => t.Tier));

        private void AddTasks(GuildType guild, SkillType skill, IEnumerable<QuestDetail> quests, RecipeCatalog recipes)
        {
            // The lowest-level recipe that makes an item sets the task's level (and is the one a crafter would pick).
            var recipeByResref = recipes.ForSkill(skill)
                .Where(r => !string.IsNullOrEmpty(r.Resref))
                .GroupBy(r => r.Resref, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Level).ThenBy(r => r.Type.ToString(), StringComparer.Ordinal).First(), StringComparer.OrdinalIgnoreCase);
            var fishLevelByResref = Enum.GetValues<FishType>()
                .Where(f => f != FishType.Invalid)
                .Select(f => f.GetAttribute<FishType, FishAttribute>())
                .Where(f => !string.IsNullOrEmpty(f.Resref))
                .GroupBy(f => f.Resref, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Level, StringComparer.OrdinalIgnoreCase);

            foreach (var quest in quests.Where(q => q.GuildType == guild).OrderBy(q => q.QuestId, StringComparer.Ordinal))
            {
                var objective = quest.States[1].GetObjectives().OfType<CollectItemObjective>().FirstOrDefault();
                if (objective == null)
                    continue;

                var kind = GuildTaskKind.Other;
                int? level = null;
                RecipeEntry recipe = null;
                if (objective.ProducerRequirement == CollectItemProducerRequirementType.PlayerProduced &&
                    recipeByResref.TryGetValue(objective.Resref, out recipe))
                {
                    kind = GuildTaskKind.Crafted;
                    level = recipe.Level;
                }
                else if (fishLevelByResref.TryGetValue(objective.Resref, out var fishLevel))
                {
                    kind = GuildTaskKind.Caught;
                    level = fishLevel;
                }

                _tasks.Add(new GuildTask(
                    guild,
                    skill,
                    quest.QuestId,
                    quest.GuildRank,
                    objective.Resref,
                    objective.Quantity,
                    kind,
                    level,
                    recipe,
                    quest.Rewards.OfType<GoldReward>().Sum(r => r.Amount),
                    quest.Rewards.OfType<GPReward>().Sum(r => r.Amount)));
            }
        }
    }
}
