using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public sealed partial class QuestConversationTests
{
    private static readonly GuildType[] XpCraftingGuilds =
    {
        GuildType.SmitheryGuild, GuildType.EngineeringGuild, GuildType.FabricationGuild, GuildType.AgricultureGuild
    };

    /// <summary>Active recipe levels by skill and resref, from the recipe definitions: the lowest level of the recipes that make an item.</summary>
    private static Func<SkillType, string, int?> RecipeLevelsFromDefinitions()
    {
        var levels = new Dictionary<(SkillType, string), int>();
        foreach (var type in typeof(IRecipeListDefinition).Assembly.GetTypes()
                     .Where(t => typeof(IRecipeListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            foreach (var recipe in ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes().Values.Where(r => r.IsActive && !string.IsNullOrEmpty(r.Resref)))
            {
                var key = (recipe.Skill, recipe.Resref.ToLowerInvariant());
                levels[key] = levels.TryGetValue(key, out var known) ? Math.Min(known, recipe.Level) : recipe.Level;
            }
        }

        return (skill, resref) => levels.TryGetValue((skill, resref.ToLowerInvariant()), out var level) ? level : null;
    }

    [Test]
    public void EveryCraftingGuildTaskGivesXpForItsCollectedItem_AndNoOtherTaskDoes()
    {
        using var scope = new AssertionScope();
        var quests = BuildQuests().Values.Where(quest => quest.GuildType != GuildType.Invalid).ToArray();

        foreach (var quest in quests)
        {
            var rewards = quest.Rewards.OfType<GuildTaskXpReward>().ToArray();
            if (!XpCraftingGuilds.Contains(quest.GuildType))
            {
                rewards.Should().BeEmpty($"{quest.QuestId} is a Hunter's Guild task");
                continue;
            }

            rewards.Should().ContainSingle(quest.QuestId);
            var objective = quest.States[1].GetObjectives().OfType<CollectItemObjective>().Single();
            var reward = rewards.Single();
            reward.Guild.Should().Be(quest.GuildType, quest.QuestId);
            reward.Resref.Should().Be(objective.Resref, quest.QuestId);
            reward.Quantity.Should().Be(objective.Quantity, quest.QuestId);
            reward.PlayerProduced.Should().Be(objective.ProducerRequirement == CollectItemProducerRequirementType.PlayerProduced, quest.QuestId);
            reward.IsSelectable.Should().BeFalse(quest.QuestId);
        }

        quests.Count(quest => XpCraftingGuilds.Contains(quest.GuildType)).Should().BeGreaterThan(600);
        quests.Where(quest => quest.GuildType == GuildType.HuntersGuild).Should().NotBeEmpty();
    }

    [Test]
    public void EveryCraftingGuildTaskResolvesToALevel()
    {
        using var scope = new AssertionScope();
        var recipeLevel = RecipeLevelsFromDefinitions();

        foreach (var quest in BuildQuests().Values.Where(quest => XpCraftingGuilds.Contains(quest.GuildType)))
        {
            var reward = quest.Rewards.OfType<GuildTaskXpReward>().Single();
            var level = GuildTaskXp.TaskLevel(reward.Guild, reward.Resref, reward.PlayerProduced, recipeLevel, GuildTaskXp.FishLevel);

            level.Should().NotBeNull($"{quest.QuestId} ({reward.Resref}) must resolve to a recipe or fish level");
        }
    }

    [Test]
    public void ACatchTaskUsesTheFishesLevel_NotARecipes()
    {
        var recipeLevel = RecipeLevelsFromDefinitions();
        var catchTask = BuildQuests()["agr_tsk_017"];
        var reward = catchTask.Rewards.OfType<GuildTaskXpReward>().Single();

        reward.Resref.Should().Be("moat_carp");
        reward.Quantity.Should().Be(4);
        reward.PlayerProduced.Should().BeFalse();
        GuildTaskXp.TaskLevel(reward.Guild, reward.Resref, reward.PlayerProduced, recipeLevel, GuildTaskXp.FishLevel).Should().Be(0);

        // At Agriculture rank 0 a fish of level 0 pays 600, so four fish pay 50% of that each.
        GuildTaskXp.Amount(0, 0, reward.Quantity).Should().Be(1200);
    }

    [Test]
    public void ACraftedTaskUsesTheRecipesLevel()
    {
        var recipeLevel = RecipeLevelsFromDefinitions();
        var task = BuildQuests()["smth_tsk_200"];
        var reward = task.Rewards.OfType<GuildTaskXpReward>().Single();

        reward.PlayerProduced.Should().BeTrue();
        var level = GuildTaskXp.TaskLevel(reward.Guild, reward.Resref, true, recipeLevel, GuildTaskXp.FishLevel);
        level.Should().BeInRange(11, 19, "tier 2 asks for the recipes of levels 11-19");
    }

    [Test]
    public void TheGuildTaskRewardsOtherThanXpAreUnchanged()
    {
        // Credits, guild points and the tier of every crafting guild task are pinned by CraftingGuildTasks_KeepTheirCreditsGuildPointsAndRankGates;
        // the XP reward is an addition that touches neither.
        foreach (var quest in BuildQuests().Values.Where(quest => XpCraftingGuilds.Contains(quest.GuildType)))
        {
            quest.Rewards.OfType<GPReward>().Should().ContainSingle(quest.QuestId);
            quest.Rewards.OfType<GoldReward>().Should().ContainSingle(quest.QuestId);
            quest.Rewards.Should().HaveCount(3, quest.QuestId);
        }
    }
}
