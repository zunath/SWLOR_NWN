using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Space;

/// <summary>
/// The space mode read the ability activation checks make (<see cref="SpaceModeQuery"/>): a
/// player whose record names an active ship, the same answer as <see cref="SpaceService.IsPlayerInSpaceMode"/> gives for the
/// same creature and record.
/// </summary>
[TestFixture]
public class SpaceModeQueryTests
{
    private const uint Pilot = 101;
    private const uint Walker = 102;
    private const uint Unrecorded = 103;
    private const uint Npc = 104;
    private const uint Dm = 105;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private InMemoryDBService _db = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! != Npc);
        _engine.SetReturnHandler("GetIsDM", args => (uint)args[0]! == Dm);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _scope = NWScriptRuntime.Override(_engine);

        _db = new InMemoryDBService()
            .With(new Player($"uuid-{Pilot}") { ActiveShipId = Guid.NewGuid().ToString() })
            .With(new Player($"uuid-{Walker}"))
            .With(new Player($"uuid-{Dm}") { ActiveShipId = Guid.NewGuid().ToString() });
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [Test]
    public void A_player_whose_record_names_an_active_ship_is_in_space_mode()
    {
        new SpaceModeQuery(_db).IsPlayerInSpaceMode(Pilot).Should().BeTrue();
    }

    [TestCase(Walker, TestName = "A player with no active ship is not in space mode")]
    [TestCase(Unrecorded, TestName = "A player with no record is not in space mode")]
    [TestCase(Npc, TestName = "A creature that is not a player is not in space mode")]
    [TestCase(Dm, TestName = "A DM is not in space mode even with a ship on the record")]
    public void Others_are_not_in_space_mode(uint creature)
    {
        new SpaceModeQuery(_db).IsPlayerInSpaceMode(creature).Should().BeFalse();
    }

    [Test]
    public void A_creature_that_is_not_a_player_never_reads_the_database()
    {
        new SpaceModeQuery(_db).IsPlayerInSpaceMode(Npc);

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void The_query_answers_as_the_space_service_does()
    {
        var space = new SpaceService(
            abilityAura: null!, abilityCooldownVisual: null!, ai: null!, area: null!, db: _db, enmity: null!, gui: null!,
            messaging: null!, perk: null!, planet: null!, playerName: null!, property: null!, race: null!, recast: null!,
            creatureStat: null!, stat: null!, statusEffect: null!, tintMap: null!);
        var query = new SpaceModeQuery(_db);

        foreach (var creature in new[] { Pilot, Walker, Unrecorded, Npc, Dm })
            query.IsPlayerInSpaceMode(creature).Should().Be(space.IsPlayerInSpaceMode(creature), $"creature {creature}");
    }
}
