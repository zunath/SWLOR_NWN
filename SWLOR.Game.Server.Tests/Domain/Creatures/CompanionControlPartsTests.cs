#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The members the companion control parts gained when they left <c>CompanionControlService</c>: the store's register, remove and
/// try-get, the authority's answer for Combat's port, and the positioning part's resets. The behavior of the moved members is
/// covered by <see cref="CompanionControlServiceTests"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CompanionControlPartsTests
{
    private AIBench _bench = null!;
    private TestCompanionControl _service = null!;
    private uint _area;
    private AISim _master = null!;
    private AISim _companion = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new AIBench();
        _area = _bench.AddArea("field");
        _master = _bench.AddPlayer("Master", _area);
        _companion = _bench.AddCreature("Droid", _area, 1f);
        _companion.Master = _master.Id;
        _service = new TestCompanionControl(ScriptedProxy.Create<IAIService>(out _), _bench.BeastMastery);
    }

    [TearDown]
    public void TearDown() => _bench.Dispose();

    [Test]
    public void The_store_registers_a_companion_in_the_area_of_its_master()
    {
        _service.Store.IsRegisteredCompanion(_companion.Id).Should().BeFalse();

        _service.Store.Register(_companion.Id);

        _service.Store.IsRegisteredCompanion(_companion.Id).Should().BeTrue();
        _service.Store.TryGetState(_companion.Id, out var state).Should().BeTrue();
        state.LastMasterArea.Should().Be(_area);
        state.Mode.Should().Be(CompanionMode.Follow);
        state.AbilitiesEnabled.Should().BeTrue();
    }

    [Test]
    public void The_store_forgets_a_removed_companion()
    {
        _service.Store.Register(_companion.Id);

        _service.Store.Remove(_companion.Id);

        _service.Store.IsRegisteredCompanion(_companion.Id).Should().BeFalse();
        _service.Store.TryGetState(_companion.Id, out _).Should().BeFalse();
    }

    [Test]
    public void The_store_gives_a_creature_it_does_not_know_a_state_and_keeps_it()
    {
        var first = _service.Store.GetState(_companion.Id);

        _service.Store.IsRegisteredCompanion(_companion.Id).Should().BeTrue();
        _service.Store.GetState(_companion.Id).Should().BeSameAs(first);
    }

    [Test]
    public void The_authority_answers_Combats_port_from_the_store()
    {
        _service.Authority.IsRegisteredCompanion(_companion.Id).Should().BeFalse();

        _service.Store.Register(_companion.Id);

        _service.Authority.IsRegisteredCompanion(_companion.Id).Should().BeTrue();
    }

    [Test]
    public void Clearing_the_engagements_drops_every_target_and_the_progress()
    {
        var state = _service.Store.GetState(_companion.Id);
        state.AttackNearestTarget = _master.Id;
        state.OwnerAssistTarget = _master.Id;
        state.DefensiveThreats[_master.Id] = _service.Now;
        state.DefensiveReactionPending = true;
        state.TrackedTarget = _master.Id;
        state.LastDistanceToTarget = 3f;

        _service.Positioning.ClearEngagements(state);

        state.AttackNearestTarget.Should().Be(AIBench.Invalid);
        state.OwnerAssistTarget.Should().Be(AIBench.Invalid);
        state.DefensiveThreats.Should().BeEmpty();
        state.DefensiveReactionPending.Should().BeFalse();
        state.TrackedTarget.Should().Be(AIBench.Invalid);
        state.LastDistanceToTarget.Should().Be(float.MaxValue);
    }

    [Test]
    public void Resetting_the_progress_keeps_the_targets()
    {
        var state = _service.Store.GetState(_companion.Id);
        state.AttackNearestTarget = _master.Id;
        state.TrackedTarget = _master.Id;
        state.LastProgressAt = _service.Now;

        _service.Positioning.ResetProgress(state);

        state.TrackedTarget.Should().Be(AIBench.Invalid);
        state.LastProgressAt.Should().Be(default);
        state.AttackNearestTarget.Should().Be(_master.Id);
    }
}
