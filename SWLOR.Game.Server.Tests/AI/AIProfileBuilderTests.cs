using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.AI;

public class AIProfileBuilderTests
{
    private enum TestPhase
    {
        Opening,
        Frenzy
    }

    [Test]
    public void Phase_UsesProfileScopedPrivateEnumId()
    {
        var builder = new AIProfileBuilder();

        var profiles = builder
            .Create(AIProfileType.Generic)
            .Boss()
            .Phase(TestPhase.Opening)
            .EnterWhen(AIPhase.Always())
            .Wait(1f)
            .Score(1)
            .Phase(TestPhase.Frenzy)
            .EnterWhen(AIPhase.HealthAtOrBelow(50))
            .Wait(1f)
            .Score(1)
            .Build();

        profiles[AIProfileType.Generic].PhaseOrder
            .Should()
            .ContainInOrder(
                AIPhaseId.Create(AIProfileType.Generic, TestPhase.Opening),
                AIPhaseId.Create(AIProfileType.Generic, TestPhase.Frenzy));
    }

    [Test]
    public void Ability_TargetIsOptionalForInference()
    {
        var builder = new AIProfileBuilder();

        var profiles = builder
            .Create(AIProfileType.Generic)
            .Ability(FeatType.Bite)
            .Score(AIScoreBand.SingleTargetDamage)
            .Build();

        var action = profiles[AIProfileType.Generic].Actions.Single();

        action.Type.Should().Be(AIActionType.Ability);
        action.Feat.Should().Be(FeatType.Bite);
        action.TargetSelector.Should().BeNull();
    }

    [Test]
    public void TargetDefaults_CanRegisterFeatSpecificOverrides()
    {
        AITarget.RegisterDefault(FeatType.Provoke1, AITarget.AllyAttacker());

        AITarget.TryGetDefaultOverride(FeatType.Provoke1, out var selector)
            .Should()
            .BeTrue();

        selector.Should().NotBeNull();
    }

    [Test]
    public void DefaultProfiles_RegisterCoreProfiles()
    {
        Ability.CacheData();
        var profiles = new DefaultAIProfileDefinition().BuildProfiles();

        profiles.Keys.Should().Contain(new[]
        {
            AIProfileType.Generic,
            AIProfileType.DroidCompanion,
            AIProfileType.BeastCompanion
        });

        profiles[AIProfileType.BeastCompanion].Actions.Should().Contain(x => x.Type == AIActionType.AttackHighestEnmity);
    }
}
