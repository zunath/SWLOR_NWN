using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Crafting revamp workstream 11: the four crafting guilds put a completed task back on the board at once, and every other guild
/// (the Hunter's Guild) keeps the old rule that a completed task stays off the board until the next server start.
/// </summary>
public class GuildTaskBoardTests
{
    private static readonly DateTime TasksLoaded = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly GuildType[] CraftingGuilds =
    {
        GuildType.SmitheryGuild,
        GuildType.EngineeringGuild,
        GuildType.FabricationGuild,
        GuildType.AgricultureGuild
    };

    [TestCase(GuildType.SmitheryGuild)]
    [TestCase(GuildType.EngineeringGuild)]
    [TestCase(GuildType.FabricationGuild)]
    [TestCase(GuildType.AgricultureGuild)]
    public void CraftingGuild_ShowsATaskAgainRightAfterItWasCompleted(GuildType guild)
    {
        Guild.IsCompletedTaskHidden(guild, TasksLoaded.AddSeconds(1), TasksLoaded).Should().BeFalse();
        Guild.IsCompletedTaskHidden(guild, TasksLoaded.AddDays(3), TasksLoaded).Should().BeFalse();
        Guild.IsCompletedTaskHidden(guild, TasksLoaded, TasksLoaded).Should().BeFalse();
    }

    [Test]
    public void HuntersGuild_KeepsACompletedTaskOffTheBoardUntilTheNextServerStart()
    {
        Guild.IsCompletedTaskHidden(GuildType.HuntersGuild, TasksLoaded.AddSeconds(1), TasksLoaded).Should().BeTrue();
        Guild.IsCompletedTaskHidden(GuildType.HuntersGuild, TasksLoaded.AddDays(3), TasksLoaded).Should().BeTrue();

        // The boundary the old board used: completed at the moment the tasks loaded still counts as this boot's.
        Guild.IsCompletedTaskHidden(GuildType.HuntersGuild, TasksLoaded, TasksLoaded).Should().BeTrue();
    }

    [Test]
    public void EveryGuild_ShowsATaskAgainOnceTheServerHasRestarted()
    {
        foreach (var guild in Enum.GetValues<GuildType>())
        {
            Guild.IsCompletedTaskHidden(guild, TasksLoaded.AddSeconds(-1), TasksLoaded).Should().BeFalse(guild.ToString());
            Guild.IsCompletedTaskHidden(guild, TasksLoaded.AddDays(-30), TasksLoaded).Should().BeFalse(guild.ToString());
        }
    }

    [Test]
    public void EveryGuild_ShowsATaskThePlayerHasNeverCompleted()
    {
        foreach (var guild in Enum.GetValues<GuildType>())
            Guild.IsCompletedTaskHidden(guild, null, TasksLoaded).Should().BeFalse(guild.ToString());
    }

    [Test]
    public void EveryGuild_ShowsTasksBeforeTheBoardHasLoaded()
    {
        foreach (var guild in Enum.GetValues<GuildType>())
            Guild.IsCompletedTaskHidden(guild, TasksLoaded, null).Should().BeFalse(guild.ToString());
    }

    [Test]
    public void OnlyTheFourCraftingGuilds_AreMarkedToRefreshTheirTasksAfterCompletion()
    {
        var refreshing = Enum.GetValues<GuildType>()
            .Where(guild => guild.GetAttribute<GuildType, GuildAttribute>().TasksRefreshAfterCompletion)
            .ToArray();

        refreshing.Should().BeEquivalentTo(CraftingGuilds);
        GuildType.HuntersGuild.GetAttribute<GuildType, GuildAttribute>().TasksRefreshAfterCompletion.Should().BeFalse();
        GuildType.Invalid.GetAttribute<GuildType, GuildAttribute>().TasksRefreshAfterCompletion.Should().BeFalse();
    }

    [Test]
    public void GuildRankThresholds_AreUnchanged()
    {
        // Rank r needs this many points to reach rank r + 1. Lifting the task cap must not change the climb.
        var expected = new Dictionary<int, int>
        {
            { 0, 1000 },
            { 1, 5000 },
            { 2, 15000 },
            { 3, 30000 },
            { 4, 45000 },
            { 5, 60000 }
        };

        foreach (var (rank, points) in expected)
            Guild.GetGPRequiredForRank(rank).Should().Be(points, $"rank {rank}");
    }
}
