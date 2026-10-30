using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Economy;

/// <summary>
/// Unit tests of the guild instance: the engine is replaced by a recording fake and the database by an in-memory one,
/// so no server is needed. The guild tasks come from the quest service, which has no quests registered here; choosing
/// ten of more than ten tasks is covered by the in-engine runs that register the real quests.
/// </summary>
[TestFixture]
public class GuildServiceTests
{
    private const uint PlayerObject = 1001;
    private const string PlayerId = PlayerEngine.PlayerId;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private GuildService _service = null!;

    // The guild tasks come from the quest service behind the static Quest class, so the fixture installs a container of its own.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _service = new GuildService(_db);
        _service.LoadData();
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
        _facadeScope.Dispose();
    }

    private static IEnumerable<GuildType> ActiveGuilds() =>
        Enum.GetValues(typeof(GuildType)).Cast<GuildType>().Where(type => type != GuildType.Invalid);

    private Player PlayerWith(GuildType guild, int rank, int points)
    {
        var player = new Player { Id = PlayerId };
        player.Guilds[guild] = new PlayerGuild { Rank = rank, Points = points };
        _db.With(player);
        return player;
    }

    // ---- the cache

    [Test]
    public void LoadData_caches_the_active_guilds_and_finds_the_highest_rank()
    {
        _service.MaxRank.Should().Be(5);
        foreach (var guild in ActiveGuilds())
        {
            _service.GetGuild(guild).IsActive.Should().BeTrue();
        }
    }

    [Test]
    public void GetGuild_fails_for_a_guild_that_is_not_active()
    {
        var get = () => _service.GetGuild(GuildType.Invalid);

        get.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void The_max_rank_is_zero_until_the_data_is_loaded_and_each_instance_keeps_its_own_cache()
    {
        var fresh = new GuildService(_db);

        fresh.MaxRank.Should().Be(0);
        var get = () => fresh.GetGuild(GuildType.HuntersGuild);
        get.Should().Throw<KeyNotFoundException>("state lives on the instance, not in static fields");
    }

    [TestCase(0, 1000)]
    [TestCase(1, 5000)]
    [TestCase(2, 15000)]
    [TestCase(3, 30000)]
    [TestCase(4, 45000)]
    [TestCase(5, 60000)]
    public void GetGPRequiredForRank_returns_the_points_needed_for_each_rank(int rank, int points)
    {
        _service.GetGPRequiredForRank(rank).Should().Be(points);
    }

    [Test]
    public void GetGPRequiredForRank_fails_for_a_rank_that_does_not_exist()
    {
        var get = () => _service.GetGPRequiredForRank(6);

        get.Should().Throw<KeyNotFoundException>();
    }

    // ---- rewards

    [Test]
    public void CalculateGPReward_adds_the_rank_and_social_bonuses_to_the_base_amount()
    {
        // The perk bonus reads the perk service, which asks the database; a DM has no perks, so it adds nothing here.
        _engine.Engine.SetReturn("GetIsDM", true);
        _engine.Engine.SetReturn("GetAbilityModifier", 4);
        PlayerWith(GuildType.HuntersGuild, rank: 2, points: 6000);

        // 100 + 100 * (0.25 * rank 2) + 100 * (0.05 * social 4) = 100 + 50 + 20
        _service.CalculateGPReward(PlayerObject, GuildType.HuntersGuild, 100).Should().Be(170);
    }

    [Test]
    public void CalculateGPReward_for_a_new_member_is_the_base_amount_and_starts_their_record_in_memory()
    {
        _engine.Engine.SetReturn("GetIsDM", true);
        _engine.Engine.SetReturn("GetAbilityModifier", 0);
        var player = new Player { Id = PlayerId };
        _db.With(player);

        _service.CalculateGPReward(PlayerObject, GuildType.EngineeringGuild, 80).Should().Be(80);

        player.Guilds.Should().ContainKey(GuildType.EngineeringGuild);
        _db.Saved.Should().BeEmpty("a reward calculation does not save anything");
    }

    [Test]
    public void GiveGuildPoints_ignores_an_amount_of_zero_or_less()
    {
        PlayerWith(GuildType.HuntersGuild, 0, 0);

        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, 0);
        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, -5);

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void GiveGuildPoints_adds_the_points_saves_the_player_and_says_so()
    {
        var player = PlayerWith(GuildType.HuntersGuild, rank: 0, points: 100);

        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, 250);

        player.Guilds[GuildType.HuntersGuild].Points.Should().Be(350);
        player.Guilds[GuildType.HuntersGuild].Rank.Should().Be(0);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);
        _engine.Messages().Should().Equal("You earned 250 Hunter's Guild guild points");
    }

    [Test]
    public void GiveGuildPoints_clamps_one_award_to_a_thousand_points()
    {
        var player = PlayerWith(GuildType.HuntersGuild, rank: 1, points: 0);

        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, 5000);

        player.Guilds[GuildType.HuntersGuild].Points.Should().Be(1000);
        _engine.Messages().Should().Equal("You earned 1000 Hunter's Guild guild points");
    }

    [Test]
    public void GiveGuildPoints_starts_a_record_for_a_guild_the_player_has_not_joined()
    {
        var player = new Player { Id = PlayerId };
        _db.With(player);

        _service.GiveGuildPoints(PlayerObject, GuildType.SmitheryGuild, 40);

        player.Guilds[GuildType.SmitheryGuild].Points.Should().Be(40);
        player.Guilds[GuildType.SmitheryGuild].Rank.Should().Be(0);
    }

    [Test]
    public void GiveGuildPoints_ranks_the_player_up_once_they_have_the_points_for_the_next_rank()
    {
        var player = PlayerWith(GuildType.HuntersGuild, rank: 0, points: 900);

        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, 100);

        player.Guilds[GuildType.HuntersGuild].Points.Should().Be(1000);
        player.Guilds[GuildType.HuntersGuild].Rank.Should().Be(1);
        _engine.Messages().Should().Equal(
            "You earned 100 Hunter's Guild guild points",
            ColorToken.Green("You've reached rank 1 in the Hunter's Guild!"));
    }

    [Test]
    public void GiveGuildPoints_ranks_up_one_rank_at_a_time()
    {
        var player = PlayerWith(GuildType.HuntersGuild, rank: 0, points: 4000);

        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, 1000);

        player.Guilds[GuildType.HuntersGuild].Points.Should().Be(5000);
        player.Guilds[GuildType.HuntersGuild].Rank.Should().Be(1, "only one rank is gained per award");
    }

    [Test]
    public void GiveGuildPoints_at_the_top_rank_keeps_the_points_one_below_the_last_rank_and_does_not_rank_up()
    {
        var player = PlayerWith(GuildType.HuntersGuild, rank: 5, points: 59500);

        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, 1000);

        player.Guilds[GuildType.HuntersGuild].Points.Should().Be(59999);
        player.Guilds[GuildType.HuntersGuild].Rank.Should().Be(5);
        _engine.Messages().Should().Equal("You earned 1000 Hunter's Guild guild points");
    }

    [TestCase(58999, 59999)]
    [TestCase(59000, 59999)]
    [TestCase(58000, 59000)]
    public void GiveGuildPoints_stops_one_point_short_of_the_last_rank_threshold(int start, int expected)
    {
        var player = PlayerWith(GuildType.HuntersGuild, rank: 5, points: start);

        _service.GiveGuildPoints(PlayerObject, GuildType.HuntersGuild, 1000);

        player.Guilds[GuildType.HuntersGuild].Points.Should().Be(expected);
    }

    // ---- tasks

    [Test]
    public void The_guild_tasks_are_empty_until_they_are_refreshed()
    {
        _service.DateTasksLoaded.Should().BeNull();
        _service.GetActiveGuildTasksByRank(GuildType.HuntersGuild, 0).Should().BeEmpty();
        _service.GetAllActiveGuildTasks(GuildType.HuntersGuild).Should().BeEmpty();
    }

    [Test]
    public void RefreshGuildTasks_offers_every_task_the_quest_service_has_for_each_guild_and_rank_and_stamps_the_time()
    {
        var before = DateTime.UtcNow;

        _service.RefreshGuildTasks();

        _service.DateTasksLoaded.Should().NotBeNull().And.BeOnOrAfter(before);
        foreach (var guild in ActiveGuilds())
        {
            for (var rank = 0; rank < _service.MaxRank; rank++)
            {
                var offered = _service.GetActiveGuildTasksByRank(guild, rank);
                var available = Quest.GetQuestsByGuild(guild, rank);
                offered.Select(task => task.QuestId).Should().BeSubsetOf(available.Select(task => task.QuestId));
                offered.Count.Should().Be(Math.Min(10, available.Count));
            }

            _service.GetAllActiveGuildTasks(guild).Keys.Should().BeEquivalentTo(
                Enumerable.Range(0, _service.MaxRank).SelectMany(rank => _service.GetActiveGuildTasksByRank(guild, rank)).Select(task => task.QuestId));
        }
    }

    [Test]
    public void RefreshGuildTasks_only_runs_once()
    {
        _service.RefreshGuildTasks();
        var first = _service.DateTasksLoaded;

        _service.RefreshGuildTasks();

        _service.DateTasksLoaded.Should().Be(first);
    }

    [Test]
    public void The_top_rank_has_no_tasks_of_its_own()
    {
        _service.RefreshGuildTasks();

        var get = () => _service.GetActiveGuildTasksByRank(GuildType.HuntersGuild, _service.MaxRank);

        get.Should().Throw<KeyNotFoundException>("tasks are chosen for the ranks below the top one");
    }
}
