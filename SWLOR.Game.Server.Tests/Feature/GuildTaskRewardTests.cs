using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

public sealed partial class QuestConversationTests
{
    // Credits and guild points per task by task tier (guild rank 0-4). Crafting revamp workstream 11 lifts the task cap only,
    // so these stay as they were; changing one is a decision for the owner.
    private static readonly IReadOnlyDictionary<int, (int Credits, int GuildPoints)> CraftingGuildTaskRewards =
        new Dictionary<int, (int, int)>
        {
            { 0, (1000, 21) },
            { 1, (2500, 81) },
            { 2, (4000, 117) },
            { 3, (5500, 156) },
            { 4, (7200, 195) }
        };

    [TestCase(GuildType.SmitheryGuild)]
    [TestCase(GuildType.EngineeringGuild)]
    [TestCase(GuildType.FabricationGuild)]
    [TestCase(GuildType.AgricultureGuild)]
    public void CraftingGuildTasks_KeepTheirCreditsGuildPointsAndRankGates(GuildType guild)
    {
        using var scope = new AssertionScope();
        var tasks = BuildQuests().Values.Where(quest => quest.GuildType == guild).ToArray();
        tasks.Should().NotBeEmpty();

        foreach (var task in tasks)
        {
            CraftingGuildTaskRewards.Should().ContainKey(task.GuildRank, task.QuestId);
            var expected = CraftingGuildTaskRewards[task.GuildRank];
            task.Rewards.OfType<GoldReward>().Sum(reward => reward.Amount).Should().Be(expected.Credits, task.QuestId);
            task.Rewards.OfType<GPReward>().Sum(reward => reward.Amount).Should().Be(expected.GuildPoints, task.QuestId);
            task.Rewards.OfType<GPReward>().Should().OnlyContain(reward => reward.Guild == guild, task.QuestId);
            task.IsRepeatable.Should().BeTrue(task.QuestId);
        }

        tasks.Select(task => task.GuildRank).Distinct().OrderBy(rank => rank)
            .Should().Equal(CraftingGuildTaskRewards.Keys.OrderBy(rank => rank));
    }
}
