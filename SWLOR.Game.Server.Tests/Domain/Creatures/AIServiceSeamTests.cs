#nullable enable
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The seams of the AI instance with nothing replaced: their defaults are the original calls, so a server built from the
/// container behaves as the static classes did. The calls that need the native engine or the database are left to the
/// in-engine tests.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AIServiceSeamTests
{
    private const uint Enemy = 100;
    private const uint Friend = 101;
    private const uint Foe = 102;
    private const uint Invalid = AIBench.Invalid;

    private AIBench _bench = null!;
    private DefaultSeamsAIService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new AIBench();
        _service = new DefaultSeamsAIService(_bench.Party, _bench.BeastMastery, _bench.DMPresence);
    }

    [TearDown]
    public void TearDown()
    {
        RealEnmityTables.Clear();
        _bench.Dispose();
    }

    [Test]
    public void The_default_enmity_reads_are_the_static_enmity_tables()
    {
        RealEnmityTables.Replace(new Dictionary<uint, Dictionary<uint, int>>
        {
            [Enemy] = new Dictionary<uint, int> { [Friend] = 5, [Foe] = 9 }
        });

        _service.DefaultHighestEnmityTarget(Enemy).Should().Be(Foe);
        _service.DefaultHighestEnmityTarget(Friend).Should().Be(Invalid);
        _service.DefaultEnmityTable(Enemy).Should().BeEquivalentTo(new Dictionary<uint, int> { [Friend] = 5, [Foe] = 9 });
        _service.DefaultHasProximityEnmity(Friend, Enemy).Should().BeFalse();
    }

    [Test]
    public void The_default_enmity_changes_are_made_to_the_static_enmity_tables()
    {
        RealEnmityTables.Replace(new Dictionary<uint, Dictionary<uint, int>>
        {
            [Enemy] = new Dictionary<uint, int> { [Friend] = 5, [Foe] = 9 }
        });

        _service.DefaultRemoveEnmity(Foe, Enemy);
        _service.DefaultEnmityTable(Enemy).Keys.Should().Equal(Friend);
        _service.DefaultRemoveTrackedProximityEnmity(Friend, Enemy).Should().BeFalse("nothing tracked it as proximity enmity");

        _service.DefaultClearEnmityTable(Enemy);
        _service.DefaultEnmityTable(Enemy).Should().BeEmpty();
    }

    [Test]
    public void The_default_companion_rules_treat_an_ordinary_creature_as_no_companion()
    {
        var creature = _bench.AddCreature("creature", _bench.AddArea("area"));

        _service.DefaultIsControlledCompanion(creature.Id).Should().BeFalse();
        _service.DefaultIsControlledCompanion(Invalid).Should().BeFalse();
        _service.DefaultAreCompanionAbilitiesEnabled(creature.Id).Should().BeTrue("a creature the companion rules do not know has its abilities");
        _service.DefaultResolveHostileAbilityTarget(creature.Id, new AbilityDetail(), Invalid, Friend).Should().Be(Invalid);
    }

    [Test]
    public void The_container_builds_the_AI_instance_with_its_neighbours_and_without_touching_the_engine()
    {
        using var container = new FacadeContainer();

        container.Get<IAIService>().Should().BeOfType<AIService>();
        container.Get<IAIService>().Should().BeSameAs(container.Get<IAIService>());
    }

    [Test]
    public void Constructing_the_instance_does_no_work()
    {
        var engine = new TimelineNWScript();
        using var scope = SWLOR.NWN.API.NWScript.NWScriptRuntime.Override(engine);

        _ = new AIService(_bench.Party, _bench.BeastMastery, _bench.DMPresence);

        engine.Calls.Should().BeEmpty();
        _bench.PartyScript.Calls.Should().BeEmpty();
        _bench.BeastMasteryScript.Calls.Should().BeEmpty();
        _bench.DMPresenceScript.Calls.Should().BeEmpty();
    }
}
