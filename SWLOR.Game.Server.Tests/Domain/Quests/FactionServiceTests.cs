using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Domain.Quests;

/// <summary>
/// Unit tests of the faction instance: the engine is replaced by a recording fake and the database by an in-memory
/// one, so no server is needed.
/// </summary>
[TestFixture]
public class FactionServiceTests
{
    private const uint PlayerObject = 1001;
    private const string PlayerId = "player-uuid-1";

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private FactionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _service = new FactionService(_db);
        _service.LoadFactions();
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
    }

    // ---- the cache

    [Test]
    public void LoadFactions_caches_every_faction_except_the_invalid_one()
    {
        var expected = Enum.GetValues(typeof(FactionType)).Cast<FactionType>().Where(type => type != FactionType.Invalid);

        _service.GetAllFactions().Keys.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void GetFactionDetail_returns_the_details_of_the_faction()
    {
        _service.GetFactionDetail(FactionType.Jedi).Name.Should().Be("Jedi Order");
        _service.GetFactionDetail(FactionType.Czerka).Name.Should().Be("Czerka Corporation");
    }

    [Test]
    public void GetFactionDetail_fails_for_the_invalid_faction()
    {
        var get = () => _service.GetFactionDetail(FactionType.Invalid);

        get.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void GetAllFactions_hands_out_a_copy()
    {
        _service.GetAllFactions().Clear();

        _service.GetAllFactions().Should().NotBeEmpty();
    }

    [Test]
    public void Each_instance_keeps_its_own_cache()
    {
        new FactionService(_db).GetAllFactions().Should().BeEmpty("state lives on the instance, not in static fields");
    }

    [Test]
    public void The_limits_are_plus_and_minus_five_thousand_on_the_instance_and_the_facade()
    {
        FactionService.MinimumFaction.Should().Be(-5000);
        FactionService.MaximumFaction.Should().Be(5000);
        Faction.MinimumFaction.Should().Be(FactionService.MinimumFaction);
        Faction.MaximumFaction.Should().Be(FactionService.MaximumFaction);
    }

    // ---- standing

    [TestCase(0, 100, 100, "Your standing with Jedi Order improves.")]
    [TestCase(250, 100, 350, "Your standing with Jedi Order improves.")]
    [TestCase(-300, 100, -200, "Your standing with Jedi Order improves.")]
    [TestCase(0, -100, -100, "Your standing with Jedi Order decreases.")]
    [TestCase(4900, 100, 5000, "Your standing with Jedi Order improves.")]
    [TestCase(4950, 100, 5000, "Your standing with Jedi Order cannot possibly go higher!")]
    [TestCase(5000, 1, 5000, "Your standing with Jedi Order cannot possibly go higher!")]
    [TestCase(-4900, -100, -5000, "Your standing with Jedi Order decreases.")]
    [TestCase(-4950, -100, -5000, "Your standing with Jedi Order cannot possibly go lower!")]
    [TestCase(-5000, -1, -5000, "Your standing with Jedi Order cannot possibly go lower!")]
    public void AdjustPlayerFactionStanding_moves_the_standing_within_the_limits_and_says_so(int start, int adjustBy, int expected, string message)
    {
        var player = new Player { Id = PlayerId };
        player.Factions[FactionType.Jedi] = new PlayerFactionStanding { Standing = start };
        _db.With(player);

        _service.AdjustPlayerFactionStanding(PlayerObject, FactionType.Jedi, adjustBy);

        player.Factions[FactionType.Jedi].Standing.Should().Be(expected);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);
        _engine.Messages().Should().Equal(message);
    }

    [Test]
    public void AdjustPlayerFactionStanding_starts_a_record_for_a_faction_the_player_has_not_met()
    {
        var player = new Player { Id = PlayerId };
        _db.With(player);

        _service.AdjustPlayerFactionStanding(PlayerObject, FactionType.Smuggler, 40);

        player.Factions[FactionType.Smuggler].Standing.Should().Be(40);
        player.Factions[FactionType.Smuggler].Points.Should().Be(0);
    }

    [TestCase(false, 10)]
    [TestCase(true, 0)]
    public void AdjustPlayerFactionStanding_ignores_a_non_player_and_a_zero_change(bool isPlayer, int adjustBy)
    {
        _engine.Engine.SetReturn("GetIsPC", isPlayer);
        _db.With(new Player { Id = PlayerId });

        _service.AdjustPlayerFactionStanding(PlayerObject, FactionType.Jedi, adjustBy);

        _db.Reads.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void AdjustPlayerFactionStanding_fails_for_a_faction_that_is_not_cached_and_saves_nothing()
    {
        _db.With(new Player { Id = PlayerId });

        var adjust = () => _service.AdjustPlayerFactionStanding(PlayerObject, FactionType.Invalid, 10);

        adjust.Should().Throw<KeyNotFoundException>();
        _db.Saved.Should().BeEmpty();
    }

    // ---- points

    [TestCase(0, 25, 25, "You gained 25 points with the Sith Order faction.")]
    [TestCase(30, 5, 35, "You gained 5 points with the Sith Order faction.")]
    [TestCase(30, -10, 20, "You lost 10 points with the Sith Order faction.")]
    [TestCase(3, -10, 0, "You lost 10 points with the Sith Order faction.")]
    public void AdjustPlayerFactionPoints_changes_the_points_never_below_zero_and_says_so(int start, int adjustBy, int expected, string message)
    {
        var player = new Player { Id = PlayerId };
        player.Factions[FactionType.Sith] = new PlayerFactionStanding { Points = start, Standing = 77 };
        _db.With(player);

        _service.AdjustPlayerFactionPoints(PlayerObject, FactionType.Sith, adjustBy);

        player.Factions[FactionType.Sith].Points.Should().Be(expected);
        player.Factions[FactionType.Sith].Standing.Should().Be(77, "points never touch the standing");
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);
        _engine.Messages().Should().Equal(message);
    }

    [Test]
    public void AdjustPlayerFactionPoints_starts_a_record_for_a_faction_the_player_has_not_met()
    {
        var player = new Player { Id = PlayerId };
        _db.With(player);

        _service.AdjustPlayerFactionPoints(PlayerObject, FactionType.Mandalorian, 12);

        player.Factions[FactionType.Mandalorian].Points.Should().Be(12);
        player.Factions[FactionType.Mandalorian].Standing.Should().Be(0);
    }

    [TestCase(false, 10)]
    [TestCase(true, 0)]
    public void AdjustPlayerFactionPoints_ignores_a_non_player_and_a_zero_change(bool isPlayer, int adjustBy)
    {
        _engine.Engine.SetReturn("GetIsPC", isPlayer);
        _db.With(new Player { Id = PlayerId });

        _service.AdjustPlayerFactionPoints(PlayerObject, FactionType.Sith, adjustBy);

        _db.Reads.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void AdjustPlayerFactionPoints_fails_for_a_faction_that_is_not_cached()
    {
        _db.With(new Player { Id = PlayerId });

        var adjust = () => _service.AdjustPlayerFactionPoints(PlayerObject, FactionType.Invalid, 10);

        adjust.Should().Throw<KeyNotFoundException>();
        _db.Saved.Should().BeEmpty();
    }
}
