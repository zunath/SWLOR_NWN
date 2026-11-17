using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The two adapters Creatures gives the DM tools (P5-15a): each does what the call it replaces did, through the same service.
/// </summary>
[TestFixture]
public class DMToolAdapterTests
{
    private const uint Creature = 4001;

    [TestCase(AIProfileType.Generic)]
    [TestCase(AIProfileType.BeastCompanion)]
    public void The_AI_profile_description_prints_the_profile_the_AI_service_returns(AIProfileType profile)
    {
        var ai = ScriptedProxy.Create<IAIService>(out var script);
        script.On(nameof(IAIService.GetAIProfile), args => (uint)args[0]! == Creature ? profile : AIProfileType.Invalid);

        new AIProfileDescription(ai).DescribeAIProfile(Creature).Should().Be($"{profile}");
        script.Calls.Should().ContainSingle().Which.Member.Should().Be(nameof(IAIService.GetAIProfile));
    }

    [Test]
    public void The_DM_spawn_preparation_prepares_the_creature_through_the_spawn_service()
    {
        var spawn = ScriptedProxy.Create<ISpawnService>(out var script);
        script.On(nameof(ISpawnService.PrepareDMSpawnedCreature), _ => null);

        new DMSpawnPreparation(spawn).PrepareDMSpawnedCreature(Creature);

        script.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Creature);
        script.Calls[0].Member.Should().Be(nameof(ISpawnService.PrepareDMSpawnedCreature));
    }
}
