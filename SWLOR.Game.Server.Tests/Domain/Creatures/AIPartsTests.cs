#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The creature event part and the aggro part of the AI: the container builds them next to the core, the core hands the aggro
/// part the ally cache and the leash entry points it calls, and the default seams of each part are the original calls.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AIPartsTests : AIServiceTestBase
{
    [Test]
    public void The_container_builds_the_event_part_and_the_aggro_part_as_their_own_services()
    {
        using var container = new FacadeContainer();

        container.Get<IAICreatureEventService>().Should().BeOfType<AICreatureEventService>();
        container.Get<IAIAggroService>().Should().BeOfType<AIAggroService>();
        container.Get<IAIAggroService>().Should().BeSameAs(container.Get<IAIAggroService>());
        container.Get<IAIAggroService>().Should().BeAssignableTo<IStealthAggroListener>();
        container.Get<IAICreatureEventService>().Should().NotBeSameAs(container.Get<IAIService>());
    }

    [Test]
    public void The_core_answers_the_allies_a_creature_has_seen()
    {
        var ally = Bench.AddCreature("ally", Area, 2f);

        Service.TryGetAllies(Npc.Id, out _).Should().BeFalse("nothing has been perceived yet");

        Bench.LastPerceived = ally.Id;
        Bench.PerceptionSeen = true;
        Bench.PerceptionVanished = false;
        Service.CreaturePerception();

        Service.TryGetAllies(Npc.Id, out var allies).Should().BeTrue();
        allies.Should().Equal(ally.Id);
    }

    [Test]
    public void The_event_part_reaches_the_core_through_its_interface_for_the_leash_and_the_flags()
    {
        // The heartbeat of a creature with no enmity and no flags does nothing visible, and does not throw: the part calls
        // ProcessFlags on the core.
        Events.CreatureHeartbeat();

        Bench.Trace().Should().Contain("RestoreNPCStats(true)");
    }

    [Test]
    public void The_default_seams_of_the_event_part_are_calls_on_the_services_it_holds()
    {
        var events = new DefaultSeamsAICreatureEventService(Service);

        events.DefaultHighestEnmityTarget(Npc.Id).Should().Be(Invalid);
    }
}
