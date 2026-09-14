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
    public void ProfileOptions_ApplyFluentSettingsAndClampUnsafeValues()
    {
        var profiles = new AIProfileBuilder()
            .Create(AIProfileType.Generic)
            .Name("Test AI")
            .DecisionThrottle(-1f)
            .MaxCandidateActions(0)
            .Build();

        var profile = profiles[AIProfileType.Generic];

        profile.Name.Should().Be("Test AI");
        profile.DecisionThrottleSeconds.Should().Be(0f);
        profile.MaxCandidateActions.Should().Be(1);
        profile.IsBoss.Should().BeFalse();
    }

    [Test]
    public void Boss_RaisesCandidateLimitAndMarksProfile()
    {
        var profiles = new AIProfileBuilder()
            .Create(AIProfileType.Generic)
            .Boss()
            .Build();

        var profile = profiles[AIProfileType.Generic];

        profile.IsBoss.Should().BeTrue();
        profile.MaxCandidateActions.Should().Be(24);
    }

    [Test]
    public void RootActions_ExposeFluentConfiguration()
    {
        var targetSelector = AITarget.Self();
        AIGuard guard = _ => true;
        AIScoreCalculation score = _ => 77;

        var profiles = new AIProfileBuilder()
            .Create(AIProfileType.Generic)
            .AttackHighestEnmity()
            .Priority(5)
            .Cooldown("attack", 2f)
            .OncePerPhase()
            .MoveToTarget(3f)
            .Flee(12f)
            .ReturnHome()
            .RandomWalk()
            .Wait(4f)
            .Speak("Alert")
            .Script("ai_custom")
            .CallAllies(6f)
            .Ability(FeatType.Bite)
            .Target(targetSelector)
            .When(guard)
            .Score(score)
            .Priority(1)
            .Cooldown("bite", 9f)
            .OncePerPhase()
            .Build();

        var actions = profiles[AIProfileType.Generic].Actions;

        actions.Select(action => action.Type).Should().ContainInOrder(
            AIActionType.AttackHighestEnmity,
            AIActionType.MoveToTarget,
            AIActionType.Flee,
            AIActionType.ReturnHome,
            AIActionType.RandomWalk,
            AIActionType.Wait,
            AIActionType.Speak,
            AIActionType.Script,
            AIActionType.CallAllies,
            AIActionType.Ability);

        actions[0].DebugName.Should().Be(nameof(AIProfileBuilder.AttackHighestEnmity));
        actions[0].Priority.Should().Be(5);
        actions[0].CooldownId.Should().Be("attack");
        actions[0].CooldownSeconds.Should().Be(2f);
        actions[0].OncePerPhase.Should().BeTrue();

        actions[1].FloatValue.Should().Be(3f);
        actions[2].FloatValue.Should().Be(12f);
        actions[5].FloatValue.Should().Be(4f);
        actions[6].Text.Should().Be("Alert");
        actions[7].ScriptName.Should().Be("ai_custom");
        actions[7].DebugName.Should().Be("ai_custom");
        actions[8].FloatValue.Should().Be(6f);

        var ability = actions[9];
        ability.Feat.Should().Be(FeatType.Bite);
        ability.TargetSelector.Should().BeSameAs(targetSelector);
        ability.Guards.Should().ContainSingle().Which.Should().BeSameAs(guard);
        ability.Score(new AIContext(0, AITriggerType.Invalid, 0, new AIProfile(), new AIState(), Array.Empty<uint>()))
            .Should()
            .Be(77);
        ability.Priority.Should().Be(1);
        ability.CooldownId.Should().Be("bite");
        ability.CooldownSeconds.Should().Be(9f);
        ability.OncePerPhase.Should().BeTrue();
    }

    [Test]
    public void PhaseActions_AreAddedToActivePhaseOnly()
    {
        var profiles = new AIProfileBuilder()
            .Create(AIProfileType.Generic)
            .Wait(1f)
            .Score(1)
            .Phase(TestPhase.Opening)
            .EnterWhen(AIPhase.Always())
            .Speak("phase")
            .Score(2)
            .Build();

        var profile = profiles[AIProfileType.Generic];
        var phaseId = AIPhaseId.Create(AIProfileType.Generic, TestPhase.Opening);

        profile.Actions.Should().ContainSingle();
        profile.Actions[0].Type.Should().Be(AIActionType.Wait);
        profile.Phases[phaseId].Actions.Should().ContainSingle();
        profile.Phases[phaseId].Actions[0].Type.Should().Be(AIActionType.Speak);
        profile.Phases[phaseId].EnterCondition.Should().NotBeNull();
    }

    [Test]
    public void Create_ResetsActivePhaseAndActionForNextProfile()
    {
        var profiles = new AIProfileBuilder()
            .Create(AIProfileType.Generic)
            .Phase(TestPhase.Opening)
            .EnterWhen(AIPhase.Always())
            .Wait(1f)
            .Score(1)
            .Create(AIProfileType.BeastCompanion)
            .Wait(2f)
            .Score(2)
            .Build();

        profiles[AIProfileType.Generic].Actions.Should().BeEmpty();
        profiles[AIProfileType.Generic].Phases.Should().ContainKey(AIPhaseId.Create(AIProfileType.Generic, TestPhase.Opening));
        profiles[AIProfileType.BeastCompanion].Actions.Should().ContainSingle();
        profiles[AIProfileType.BeastCompanion].Phases.Should().BeEmpty();
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
