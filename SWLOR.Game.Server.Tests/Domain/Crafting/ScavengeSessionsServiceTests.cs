using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Crafting.Minigame.Search;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Service.Search;
using SWLOR.Game.Server.Tests.Service;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// The scavenge entry point: the service owns the one engine world, and the sessions service keeps one lifecycle, so the visits it
/// starts stay known to it. The rules are covered by <c>ScavengeLifecycleTests</c>.
/// </summary>
[TestFixture]
public class ScavengeSessionsServiceTests
{
    private const uint Player = 100;
    private const uint Site = 500;
    private const int Rank = 0;
    private const int SiteLevel = 3;

    private sealed class TestSessions : ScavengeSessionsService
    {
        private readonly IScavengeWorld _world;
        public int Created { get; private set; }

        public TestSessions(IScavengeService scavenge, IScavengeWorld world)
            : base(scavenge)
        {
            _world = world;
        }

        protected override ScavengeLifecycle CreateScavengeLifecycle(NwnScavengeWorld world)
        {
            Created++;
            var layout = Enumerable.Range(0, SearchSetup.ForRank(Rank, SiteLevel).PromisingSpots).ToArray();
            return new ScavengeLifecycle(_world, new ScriptedMinigameRandom(layout));
        }
    }

    private PlayerEngine _engine = null!;
    private FakeScavengeWorld _world = null!;
    private ScavengeService _scavenge = null!;
    private TestSessions _sessions = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _world = new FakeScavengeWorld();
        _scavenge = new ScavengeService(new InMemoryDBService());
        _sessions = new TestSessions(_scavenge, _world);
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    private static ScavengeRequest Request(uint player = Player) =>
        new(player, Site, SiteLevel, "TEST_TABLE", new LootTable { new LootTableItem("fiberp_ruined", 3, 50, false) }, Rank, 600, 0, 0);

    [Test]
    public void The_service_owns_one_world_for_the_life_of_the_container()
    {
        _scavenge.World.Should().NotBeNull().And.BeSameAs(_scavenge.World);
    }

    [Test]
    public void A_visit_is_known_to_the_service_that_started_it_and_the_lifecycle_is_built_once()
    {
        var session = _sessions.Start(Request());

        session.Should().NotBeNull();
        _sessions.IsInUse(Site).Should().BeTrue();
        _sessions.GetActiveAt(Site).Should().BeSameAs(session);
        _sessions.IsActive(session).Should().BeTrue();
        _sessions.Start(Request(player: 101)).Should().BeNull("another scavenger is at the site");
        _sessions.Created.Should().Be(1);
    }

    [Test]
    public void A_visit_ends_when_its_scavenger_dies()
    {
        var session = _sessions.Start(Request());
        _engine.Engine.SetReturn("GetLastPlayerDied", Player);

        _sessions.AbandonOnDeath();

        session.Settled.Should().BeTrue();
        _sessions.IsInUse(Site).Should().BeFalse();
    }

    [Test]
    public void A_visit_ends_when_its_scavenger_logs_out()
    {
        var session = _sessions.Start(Request());
        _engine.Engine.SetReturn("GetExitingObject", Player);

        _sessions.AbandonOnExit();

        session.Settled.Should().BeTrue();
        _sessions.IsActive(session).Should().BeFalse();
    }

    [Test]
    public void Someone_without_a_visit_who_dies_or_leaves_changes_nothing()
    {
        var session = _sessions.Start(Request());
        _engine.Engine.SetReturn("GetLastPlayerDied", 77u);
        _engine.Engine.SetReturn("GetExitingObject", 77u);

        _sessions.AbandonOnDeath();
        _sessions.AbandonOnExit();

        session.Settled.Should().BeFalse();
    }

    [Test]
    public void Quick_search_and_abandoning_go_through_the_one_lifecycle()
    {
        var session = _sessions.Start(Request());

        _sessions.CanQuickSearch(session).Should().BeTrue();
        _sessions.SecondsUntilNextAction(session).Should().BeGreaterThanOrEqualTo(0);
        _sessions.Revalidate(session).Should().BeTrue();
        _sessions.Abandon(session, ScavengeEnd.WindowClosed);

        session.Settled.Should().BeTrue();
    }
}
