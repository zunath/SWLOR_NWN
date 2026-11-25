using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Economy;

/// <summary>
/// The crafting guilds replace a task a player completes with a different task of the same rank (master #3567): the roll
/// keeps the whole shuffled rotation next to the ten-task board, each player's board is chosen from it, and a hot reload
/// carries the rotation. The quest registry is replaced by a stub that answers the two quest reads the guild makes.
/// </summary>
[TestFixture]
public class GuildServiceRotationTests
{
    private const GuildType Crafting = GuildType.SmitheryGuild;
    private const int Rank = 0;
    private const int RotationSize = 14;
    private const int BoardSize = 10;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private QuestStub _quests = null!;
    private GuildService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _ = new LocalVariables(_engine.Engine);
        _engine.Engine.SetReturn("GetModule", 1u);
        _db = new InMemoryDBService();
        _quests = DispatchProxy.Create<IQuestService, QuestStub>() as QuestStub ?? throw new InvalidOperationException();
        _service = NewService();
        _service.LoadData();
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    private GuildService NewService() =>
        new(_db, FacadeForwarder.For<IPerkService>(), (IQuestService)_quests);

    private List<QuestDetail> Register(GuildType guild, int rank, int count)
    {
        var tasks = Enumerable.Range(0, count)
            .Select(i => new QuestDetail { QuestId = $"{guild}_{rank}_{i:00}", GuildType = guild, GuildRank = rank })
            .ToList();
        _quests.ByGuild[(guild, rank)] = tasks;
        foreach (var task in tasks)
            _quests.ById[task.QuestId] = task;
        return tasks;
    }

    private static PlayerQuest Completed(DateTime when) => new() { DateLastCompleted = when, TimesCompleted = 1 };

    [Test]
    public void A_roll_of_more_than_ten_tasks_puts_ten_on_the_board_and_keeps_the_rest_in_the_rotation()
    {
        var tasks = Register(Crafting, Rank, RotationSize);

        _service.RefreshGuildTasks();

        var board = _service.GetActiveGuildTasksByRank(Crafting, Rank);
        board.Should().HaveCount(BoardSize).And.OnlyHaveUniqueItems();
        board.Select(task => task.QuestId).Should().BeSubsetOf(tasks.Select(task => task.QuestId));
        _service.GetAllActiveGuildTasks(Crafting).Keys.Should().BeEquivalentTo(board.Select(task => task.QuestId));

        var offered = _service.GetGuildTasksForPlayer(Crafting, new Dictionary<string, PlayerQuest>());
        offered.Keys.Should().BeEquivalentTo(board.Select(task => task.QuestId), "a player who completed nothing sees the board");
    }

    [Test]
    public void A_crafting_guild_player_gets_a_different_task_in_place_of_a_completed_one()
    {
        Register(Crafting, Rank, RotationSize);
        _service.RefreshGuildTasks();
        var board = _service.GetActiveGuildTasksByRank(Crafting, Rank).Select(task => task.QuestId).ToList();
        var done = board[2];
        var quests = new Dictionary<string, PlayerQuest> { [done] = Completed(_service.DateTasksLoaded!.Value.AddMinutes(1)) };

        var offered = _service.GetGuildTasksForPlayer(Crafting, quests);

        offered.Should().HaveCount(BoardSize);
        offered.Keys.Should().NotContain(done);
        offered.Keys.Except(board).Should().ContainSingle("one task of the rotation comes in from beyond the board");
    }

    [Test]
    public void A_rank_with_no_spare_tasks_keeps_its_board_full_for_a_crafting_guild_player()
    {
        Register(Crafting, Rank, 4);
        _service.RefreshGuildTasks();
        var board = _service.GetActiveGuildTasksByRank(Crafting, Rank).Select(task => task.QuestId).ToList();
        var quests = new Dictionary<string, PlayerQuest> { [board[0]] = Completed(_service.DateTasksLoaded!.Value.AddMinutes(1)) };

        _service.GetGuildTasksForPlayer(Crafting, quests).Keys.Should().BeEquivalentTo(board);
    }

    [Test]
    public void Other_guilds_and_boards_that_are_not_rolled_yet_give_the_shared_list()
    {
        Register(Crafting, Rank, RotationSize);
        Register(GuildType.HuntersGuild, Rank, RotationSize);
        var quests = new Dictionary<string, PlayerQuest>();

        _service.GetGuildTasksForPlayer(Crafting, quests).Should().BeEmpty("nothing is rolled before the quests register");

        _service.RefreshGuildTasks();
        var hunters = _service.GetAllActiveGuildTasks(GuildType.HuntersGuild);
        hunters.Should().HaveCount(BoardSize);
        hunters.Keys.Should().BeEquivalentTo(_service.GetActiveGuildTasksByRank(GuildType.HuntersGuild, Rank).Select(task => task.QuestId));
        _service.GetGuildTasksForPlayer(GuildType.HuntersGuild, new Dictionary<string, PlayerQuest> { ["x"] = Completed(DateTime.UtcNow) })
            .Keys.Should().BeEquivalentTo(hunters.Keys, "a completed task stays off a non-crafting board");
    }

    [Test]
    public void A_reload_carries_the_rotation_so_a_completed_task_is_still_replaced()
    {
        Register(Crafting, Rank, RotationSize);
        _service.RefreshGuildTasks();
        var board = _service.GetActiveGuildTasksByRank(Crafting, Rank).Select(task => task.QuestId).ToList();
        var loaded = _service.DateTasksLoaded!.Value;

        _service.CarryGuildTasks();
        var reloaded = NewService();
        reloaded.LoadData();
        reloaded.RefreshGuildTasks();
        reloaded.AdoptGuildTasks();

        reloaded.DateTasksLoaded.Should().Be(loaded);
        reloaded.GetActiveGuildTasksByRank(Crafting, Rank).Select(task => task.QuestId).Should().Equal(board);
        var quests = new Dictionary<string, PlayerQuest> { [board[0]] = Completed(loaded.AddMinutes(1)) };
        var offered = reloaded.GetGuildTasksForPlayer(Crafting, quests);
        offered.Should().HaveCount(BoardSize);
        offered.Keys.Should().NotContain(board[0]);
        offered.Keys.Except(board).Should().ContainSingle();
    }

    [Test]
    public void A_carry_from_a_build_without_a_rotation_falls_back_to_the_carried_board_and_the_new_roll()
    {
        var board = Register(Crafting, Rank, RotationSize).Take(BoardSize).Select(task => task.QuestId).ToList();
        var carry = new Newtonsoft.Json.Linq.JObject
        {
            ["loaded"] = DateTime.UtcNow.AddHours(-1),
            ["tasks"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
            {
                ["guild"] = (int)Crafting,
                ["rank"] = Rank,
                ["quests"] = new Newtonsoft.Json.Linq.JArray(board.Append("retired_task")),
            }),
        };
        ReloadCarry.Put("guildTasks", carry.ToString(Newtonsoft.Json.Formatting.None));

        _service.RefreshGuildTasks();
        _service.AdoptGuildTasks();

        _service.GetActiveGuildTasksByRank(Crafting, Rank).Select(task => task.QuestId).Should().Equal(board, "a retired quest is dropped");
        var offered = _service.GetGuildTasksForPlayer(Crafting, new Dictionary<string, PlayerQuest>());
        offered.Keys.Should().BeEquivalentTo(board);
    }

    /// <summary>An <see cref="IQuestService"/> that answers the guild's two reads from tables and fails on anything else.</summary>
    public class QuestStub : DispatchProxy
    {
        public Dictionary<(GuildType, int), List<QuestDetail>> ByGuild { get; } = new();
        public Dictionary<string, QuestDetail> ById { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod!.Name)
            {
                case nameof(IQuestService.GetQuestsByGuild):
                    return ByGuild.TryGetValue(((GuildType)args![0]!, (int)args[1]!), out var tasks) ? tasks.ToList() : new List<QuestDetail>();
                case nameof(IQuestService.GetQuestByIdOrDefault):
                    return ById.GetValueOrDefault((string)args![0]!);
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }
}
