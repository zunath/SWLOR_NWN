using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.ProspectingService;
using SWLOR.Game.Server.Tests.Service.Prospecting;
using SWLOR.Game.Server.Tests.Service;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// The Prospecting entry point: the service owns the one engine world, and the sessions service keeps one lifecycle, so the
/// sessions it opens stay known to it. The rules are covered by <c>ProspectingLifecycleTests</c>.
/// </summary>
[TestFixture]
public class ProspectingSessionsServiceTests : SkillFacadeFixture
{
    private const uint Player = 11;
    private const uint Harvester = 22;
    private const uint NodeId = 33;

    private sealed class TestSessions : ProspectingSessionsService
    {
        private readonly IProspectingWorld _world;
        public int Created { get; private set; }

        public TestSessions(IProspectingService prospecting, IProspectingWorld world)
            : base(prospecting)
        {
            _world = world;
        }

        protected override ProspectingLifecycle CreateProspectingLifecycle(NwnProspectingWorld world)
        {
            Created++;
            return new ProspectingLifecycle(_world, new ScriptedMinigameRandom());
        }
    }

    private PlayerEngine _engine = null!;
    private FakeProspectingWorld _world = null!;
    private ProspectingService _prospecting = null!;
    private TestSessions _sessions = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _world = new FakeProspectingWorld();
        _world.Charges[Harvester] = 50;
        _world.SaveNode(NodeId, Node(40, "").Serialize());
        _prospecting = new ProspectingService(new InMemoryDBService());
        _sessions = new TestSessions(_prospecting, _world);
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    private static ProspectingRequest Request(uint player = Player) =>
        new(player, Harvester, NodeId, "HARVESTING_PLAGIONITE", 3, 25, new System.Numerics.Vector3(10f, 20f, 0f));

    private ProspectingSession Begin()
    {
        var begin = _sessions.Begin(Request());
        begin.Status.Should().Be(ProspectingBeginStatus.Opened);
        return begin.Session;
    }

    [Test]
    public void The_service_owns_one_world_for_the_life_of_the_container()
    {
        _prospecting.World.Should().NotBeNull().And.BeSameAs(_prospecting.World);
    }

    [Test]
    public void The_lifecycle_is_built_once_over_the_service_world_and_keeps_the_sessions()
    {
        var session = Begin();

        _sessions.IsActive(session).Should().BeTrue();
        _sessions.IsInUseByAnother(NodeId, 12).Should().BeTrue();
        _sessions.IsInUseByAnother(NodeId, Player).Should().BeFalse();
        _sessions.Created.Should().Be(1);
    }

    [Test]
    public void A_session_ends_when_its_player_dies()
    {
        var session = Begin();
        _engine.Engine.SetReturn("GetLastPlayerDied", Player);

        _sessions.AbandonOnDeath();

        session.Settled.Should().BeTrue();
        session.Abandoned.Should().Be(ProspectingEnd.Died);
        _sessions.IsActive(session).Should().BeFalse();
    }

    [Test]
    public void A_session_ends_when_its_player_leaves_and_the_node_is_released()
    {
        var session = Begin();
        _engine.Engine.SetReturn("GetExitingObject", Player);

        _sessions.AbandonOnExit();

        session.Abandoned.Should().Be(ProspectingEnd.Left);
        _sessions.IsInUseByAnother(NodeId, 12).Should().BeFalse();
    }

    [Test]
    public void A_player_without_a_session_who_dies_or_leaves_changes_nothing()
    {
        var session = Begin();
        _engine.Engine.SetReturn("GetLastPlayerDied", 77u);
        _engine.Engine.SetReturn("GetExitingObject", 77u);

        _sessions.AbandonOnDeath();
        _sessions.AbandonOnExit();

        session.Settled.Should().BeFalse();
    }

    [Test]
    public void Abandoning_through_the_service_settles_the_session_and_the_xp_per_item_is_the_lifecycle_answer()
    {
        var session = Begin();

        _sessions.XpPerItem(session).Should().BeGreaterThanOrEqualTo(0);
        _sessions.Revalidate(session).Should().BeTrue();
        _sessions.Abandon(session, ProspectingEnd.WindowClosed);

        session.Settled.Should().BeTrue();
    }
}
