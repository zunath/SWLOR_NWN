using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Crafting revamp workstream 11: the four crafting guilds replace a completed task with a different task of the same rank, and every
/// other guild (the Hunter's Guild) keeps the old rule that a completed task stays off the board until the next server start.
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
    public void CraftingGuild_LeavesACompletedTaskToTheRotationInsteadOfHidingItUntilRestart(GuildType guild)
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

    // A crafting guild rank with 16 tasks (the Engineering Guild's size) and the 10-task board.
    private static readonly string[] Rotation = Enumerable.Range(0, 16).Select(i => $"eng_tsk_{i:00}").ToArray();
    private const int BoardSize = 10;

    private static PlayerQuest Completed(DateTime when) => new() { DateLastCompleted = when, TimesCompleted = 1 };
    private static PlayerQuest InProgress() => new() { CurrentState = 1 };

    private static List<string> Board(Dictionary<string, PlayerQuest> quests) =>
        Guild.SelectPlayerTaskIds(Rotation, BoardSize, quests, TasksLoaded);

    [Test]
    public void CraftingBoard_OffersTheSharedTenTasksToAPlayerWhoHasCompletedNone()
    {
        Board(new Dictionary<string, PlayerQuest>()).Should().Equal(Rotation.Take(BoardSize));
    }

    [Test]
    public void CraftingBoard_ReplacesACompletedTaskWithADifferentTaskOfTheSameRank()
    {
        var quests = new Dictionary<string, PlayerQuest> { [Rotation[3]] = Completed(TasksLoaded.AddMinutes(5)) };

        var board = Board(quests);

        board.Should().HaveCount(BoardSize);
        board.Should().NotContain(Rotation[3]);
        board.Should().Contain(Rotation[BoardSize]);
    }

    [Test]
    public void CraftingBoard_NeverOffersTheTaskJustCompletedWhileTheRankHasOthers()
    {
        // Hand in the first task on the board over and over, as the reporter did with Ion Cannon IV.
        var quests = new Dictionary<string, PlayerQuest>();
        var when = TasksLoaded;
        var handedIn = new List<string>();
        for (var i = 0; i < Rotation.Length * 3; i++)
        {
            var board = Board(quests);
            board.Should().HaveCount(BoardSize);
            if (handedIn.Count > 0)
                board.Should().NotContain(handedIn[^1], $"hand-in {i}");

            when = when.AddMinutes(1);
            quests[board[0]] = Completed(when);
            handedIn.Add(board[0]);
        }

        // Every task of the rank comes up in turn before any repeats.
        handedIn.Take(Rotation.Length).Should().OnlyHaveUniqueItems();
        handedIn.Take(Rotation.Length).Should().BeEquivalentTo(Rotation);
    }

    [Test]
    public void CraftingBoard_KeepsATaskThePlayerHasInProgress()
    {
        // The player took a replacement task, then completed more tasks: the accepted one must stay on the board (not show as expired).
        var quests = new Dictionary<string, PlayerQuest> { [Rotation[15]] = InProgress() };
        for (var i = 0; i < 12; i++)
            quests[Rotation[i]] = Completed(TasksLoaded.AddMinutes(i + 1));

        var board = Board(quests);

        board.Should().Contain(Rotation[15]);
        board.Should().HaveCount(BoardSize);
        board.Should().NotContain(Rotation[11]);
    }

    [Test]
    public void CraftingBoard_ShowsEveryTaskInProgressEvenPastTheBoardSize()
    {
        var quests = Rotation.ToDictionary(id => id, _ => InProgress());

        Board(quests).Should().Equal(Rotation);
    }

    [Test]
    public void CraftingBoard_TreatsTasksCompletedBeforeTheServerStartedAsAvailable()
    {
        // Existing player data: tasks completed on earlier boots count as available, so nothing needs staff action after deploy.
        var quests = Rotation.ToDictionary(id => id, _ => Completed(TasksLoaded.AddDays(-2)));

        Board(quests).Should().Equal(Rotation.Take(BoardSize));
    }

    [Test]
    public void CraftingBoard_WithNoSpareTasks_KeepsTheBoardFull()
    {
        // A rank with no more tasks than the board holds has nothing to rotate in, so a completed task stays offered, listed last.
        var rotation = Rotation.Take(BoardSize).ToArray();
        var quests = new Dictionary<string, PlayerQuest> { [rotation[0]] = Completed(TasksLoaded.AddMinutes(1)) };

        Guild.SelectPlayerTaskIds(rotation, BoardSize, quests, TasksLoaded).Should().Equal(rotation.Skip(1).Append(rotation[0]));
    }

    [Test]
    public void NonCraftingGuild_BoardIsTheSharedList()
    {
        var quests = new Dictionary<string, PlayerQuest>();
        Guild.GetGuildTasksForPlayer(GuildType.HuntersGuild, quests)
            .Should().BeEquivalentTo(Guild.GetAllActiveGuildTasks(GuildType.HuntersGuild));
    }
}
