using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Service;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// The crafting session entry point: it loads the player's record from the database and runs the lifecycle rules against an
/// in-memory engine world. The rules themselves are covered by <c>CraftSessionLifecycleTests</c>.
/// </summary>
[TestFixture]
public class CraftSessionsServiceTests
{
    private const RecipeType TestRecipe = RecipeType.VenomCoating1;
    private const uint Handle = FakeCraftSessionWorld.PlayerHandle;

    private sealed class TestSessions : CraftSessionsService
    {
        private readonly CraftSessionLifecycle _lifecycle;

        public TestSessions(IDBService db, ICraftService craft, CraftSessionLifecycle lifecycle)
            : base(db, craft)
        {
            _lifecycle = lifecycle;
        }

        protected override CraftSessionLifecycle Lifecycle => _lifecycle;
    }

    // The craft rules reach the crafting service through its static facade, which resolves from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private FakeCraftSessionWorld _world = null!;
    private Player _player = null!;
    private TestSessions _service = null!;

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
        _world = new FakeCraftSessionWorld();
        _world.Recipes[TestRecipe] = new RecipeDetail
        {
            Resref = "test_item",
            Skill = SkillType.Smithery,
            Level = 10,
            Components = new Dictionary<string, int> { ["ore"] = 3 },
            EnhancementType = RecipeEnhancementType.Armor
        };
        _world.Inventory["ore"] = 5;
        _player = new Player(PlayerEngine.PlayerId);
        _player.Skills[SkillType.Smithery] = new PlayerSkill { Rank = 10 };
        _service = new TestSessions(_db, CraftServices.Create(_db), new CraftSessionLifecycle(_world, new ScriptedMinigameRandom(Enumerable.Repeat(100, 50).ToArray())));
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
        _facadeScope.Dispose();
    }

    private CraftSessionStartRequest Request() => new(TestRecipe, SWLOR.NWN.API.NWScript.NWScript.OBJECT_INVALID, new uint[0]);

    private CraftSession StartCraft()
    {
        _db.With(_player);
        var result = _service.Start(Handle, Request());
        result.Succeeded.Should().BeTrue(result.Message);
        return result.Session;
    }

    [Test]
    public void Every_call_answers_without_a_player_record()
    {
        var session = new CraftSession("session-1", TestRecipe, null!);

        var started = _service.Start(Handle, Request());
        started.Succeeded.Should().BeFalse();
        started.Message.Should().Be("Your character could not be loaded.");
        _service.Succeed(Handle, session, new MinigameMeter(100, 100), 5).Should().BeNull();
        _service.Fail(Handle, CraftSessionEndReason.Broken).Should().Be(CraftSettlement.None);
        _service.SettleLeftover(Handle).Should().Be(CraftSettlement.None);
        _service.RecoverAtLogin(Handle).Should().Be(CraftSettlement.None);
        _service.GrantPendingXp(Handle).Should().BeFalse();
        _service.IsCrafting(Handle).Should().BeFalse();
        _service.IsActive(Handle, session).Should().BeFalse();
        _service.Invoking(service => service.RecordActions(Handle, session, 3)).Should().NotThrow();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void A_started_craft_is_saved_on_the_record_and_is_the_active_session()
    {
        var session = StartCraft();

        _service.IsCrafting(Handle).Should().BeTrue();
        _service.IsActive(Handle, session).Should().BeTrue();
        _service.IsActive(Handle, new CraftSession("another", TestRecipe, null!)).Should().BeFalse();
        _player.ActiveCraftSession.SessionId.Should().Be(session.SessionId);
    }

    [Test]
    public void RecordActions_saves_the_actions_the_craft_has_resolved()
    {
        var session = StartCraft();

        _service.RecordActions(Handle, session, 7);

        _player.ActiveCraftSession.ActionsTaken.Should().Be(7);
    }

    [Test]
    public void Succeed_creates_the_item_once_and_ends_the_session()
    {
        var session = StartCraft();
        _world.AdvanceByActions(10);

        var first = _service.Succeed(Handle, session, new MinigameMeter(100, 100), 10);
        var second = _service.Succeed(Handle, session, new MinigameMeter(100, 100), 10);

        first.Should().NotBeNull();
        second.Should().BeNull("the session already ended, so a repeated request does nothing");
        _service.IsCrafting(Handle).Should().BeFalse();
    }

    [Test]
    public void Fail_settles_the_saved_craft_as_a_failure()
    {
        var session = StartCraft();

        var settlement = _service.Fail(Handle, CraftSessionEndReason.Broken, 4);

        settlement.Settled.Should().BeTrue();
        _service.IsCrafting(Handle).Should().BeFalse();
        _service.IsActive(Handle, session).Should().BeFalse();
    }

    [Test]
    public void SettleLeftover_and_RecoverAtLogin_settle_a_craft_nothing_is_playing()
    {
        StartCraft();
        _service.SettleLeftover(Handle).Settled.Should().BeTrue();

        _world.Inventory["ore"] = 5;
        StartCraft();
        _service.RecoverAtLogin(Handle).Settled.Should().BeTrue();
        _service.IsCrafting(Handle).Should().BeFalse();
    }

    [Test]
    public void GrantPendingXp_gives_the_xp_a_craft_earned_while_the_player_was_dead()
    {
        _player.PendingCraftXp = new Dictionary<SkillType, int> { [SkillType.Smithery] = 40 };
        _db.With(_player);

        _service.GrantPendingXp(Handle).Should().BeTrue();

        _world.XpGiven.Should().Equal((SkillType.Smithery, 40));
    }

    [Test]
    public void The_default_lifecycle_runs_against_the_session_world_of_the_crafting_service()
    {
        var craft = CraftServices.Create(_db);
        var service = new CraftSessionsService(_db, craft);

        service.IsCrafting(Handle).Should().BeFalse();
        service.Start(Handle, Request()).Message.Should().Be("Your character could not be loaded.");
    }
}
