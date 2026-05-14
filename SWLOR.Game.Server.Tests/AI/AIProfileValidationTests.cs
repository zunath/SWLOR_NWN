using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.AI;

public class AIProfileValidationTests
{
    [Test]
    public void DefaultProfileAbilityActions_AllReferenceRegisteredAbilities()
    {
        Ability.CacheData();
        var registeredFeats = Ability.GetAllAbilityDetails().Keys.ToHashSet();

        var profiles = new DefaultAIProfileDefinition().BuildProfiles();
        var abilityActions = profiles.Values
            .SelectMany(profile => profile.Actions.Concat(profile.Phases.Values.SelectMany(phase => phase.Actions)))
            .Where(action => action.Type == AIActionType.Ability)
            .Select(action => action.Feat)
            .Distinct();

        abilityActions.Should().OnlyContain(feat => registeredFeats.Contains(feat));
    }

    [Test]
    public void DefaultProfiles_ExposeEveryRegisteredAbilityFeat()
    {
        Ability.CacheData();
        var registeredFeats = Ability.GetAllAbilityDetails().Keys.ToHashSet();
        var profiles = new DefaultAIProfileDefinition().BuildProfiles();

        foreach (var profile in profiles.Values)
        {
            var profileFeats = profile.Actions
                .Concat(profile.Phases.Values.SelectMany(phase => phase.Actions))
                .Where(action => action.Type == AIActionType.Ability)
                .Select(action => action.Feat)
                .ToHashSet();

            registeredFeats.Except(profileFeats).Should().BeEmpty();
        }
    }

    [Test]
    public void NPCAI_CacheProfilesLoadsAndValidatesDefaultProfiles()
    {
        Ability.CacheData();

        NPCAI.CacheProfiles();

        NPCAI.Profiles.Keys.Should().Contain(new[]
        {
            AIProfileType.Generic,
            AIProfileType.DroidCompanion,
            AIProfileType.BeastCompanion
        });

        FluentActions.Invoking(NPCAI.ValidateProfiles).Should().NotThrow();
    }

    [Test]
    public void NPCHostileSingleTargetAbilities_RequireEnemyTargets()
    {
        Ability.CacheData();
        var feats = new[]
        {
            FeatType.Provoke1,
            FeatType.Bite,
            FeatType.Spikes,
            FeatType.Talon,
            FeatType.Venom
        };

        foreach (var feat in feats)
        {
            var ability = Ability.GetAbilityDetail(feat);
            ability.IsHostileAbility.Should().BeTrue($"{feat} applies harmful effects to its target");
            ability.IsSingleTargetAbility.Should().BeTrue($"{feat} applies directly to one target");
            ability.RequiresTarget.Should().BeTrue($"{feat} must not fall back to targeting the caster");
        }
    }

    [Test]
    public void NPCHostileAreaAbilities_RequireEnemyTargetsForAISelection()
    {
        Ability.CacheData();
        var feats = new[]
        {
            FeatType.Provoke2,
            FeatType.Earthquake,
            FeatType.GreaterEarthquake,
            FeatType.FireBreath,
            FeatType.FlameBlast,
            FeatType.Roar,
            FeatType.Screech
        };

        foreach (var feat in feats)
        {
            var ability = Ability.GetAbilityDetail(feat);
            ability.IsHostileAbility.Should().BeTrue($"{feat} affects enemies");
            ability.IsAreaAbility.Should().BeTrue($"{feat} affects an area");
            ability.RequiresTarget.Should().BeTrue($"{feat} should only be selected when an enemy target exists");
            ability.MaxRange.Should().BeGreaterThan(0f);
        }
    }

    [Test]
    public void NPCSelfBuffAbilities_DoNotRequireTargets()
    {
        Ability.CacheData();
        var ability = Ability.GetAbilityDetail(FeatType.IronShell);

        ability.IsHostileAbility.Should().BeFalse();
        ability.RequiresTarget.Should().BeFalse();
    }

    [Test]
    public void BuilderBossPhases_AllHaveEntryConditions()
    {
        var profiles = new AIProfileBuilder()
            .Create(AIProfileType.Generic)
            .Boss()
            .Phase(TestPhase.Opening)
            .EnterWhen(AIPhase.Always())
            .Wait(1f)
            .Score(1)
            .Build();

        profiles[AIProfileType.Generic].Phases.Values
            .Should()
            .OnlyContain(phase => phase.EnterCondition != null);
    }

    private enum TestPhase
    {
        Opening
    }
}
