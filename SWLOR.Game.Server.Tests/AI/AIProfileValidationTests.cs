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
    public void DefaultProfiles_CanEvaluateEveryRegisteredAbilityAction()
    {
        Ability.CacheData();
        var registeredAbilities = Ability.GetAllAbilityDetails();
        var profiles = new DefaultAIProfileDefinition().BuildProfiles();

        foreach (var profile in profiles.Values)
        {
            var abilityActions = profile.Actions
                .Concat(profile.Phases.Values.SelectMany(phase => phase.Actions))
                .Where(action => action.Type == AIActionType.Ability)
                .ToArray();

            profile.MaxCandidateActions.Should().BeGreaterThanOrEqualTo(
                abilityActions.Length,
                $"{profile.Type} must not drop registered ability actions before AI can score them");

            foreach (var action in abilityActions)
            {
                registeredAbilities.Should().ContainKey(action.Feat);
                var ability = registeredAbilities[action.Feat];

                (ability.AITargetSelector ?? AITarget.InferDefault(action.Feat, ability))
                    .Should()
                    .NotBeNull($"{action.Feat} must have an AI target selector");

                (ability.AIScore ?? AIScore.Ability(ability))
                    .Should()
                    .NotBeNull($"{action.Feat} must have an AI score calculation");
            }
        }
    }

    [Test]
    public void DefaultProfiles_UseAbilitySpecificAIMetadata()
    {
        Ability.CacheData();
        var abilities = Ability.GetAllAbilityDetails();
        var profiles = new DefaultAIProfileDefinition().BuildProfiles();
        var beastActions = profiles[AIProfileType.BeastCompanion].Actions
            .Where(action => action.Type == AIActionType.Ability)
            .ToDictionary(action => action.Feat);

        foreach (var feat in new[]
                 {
                     FeatType.Anger1,
                     FeatType.Anger2,
                     FeatType.GuardingRoar1,
                     FeatType.GuardingRoar2,
                     FeatType.GuardingRoar3
                 })
        {
            var ability = abilities[feat];
            var action = beastActions[feat];

            action.TargetSelector.Should().BeSameAs(ability.AITargetSelector);
            action.Score.Should().BeSameAs(ability.AIScore);
        }
    }

    [Test]
    public void DroidProfile_SteelShoulderUsesTheAbilityTargetSelector()
    {
        Ability.CacheData();
        var ability = Ability.GetAbilityDetail(FeatType.TwinGuardStance1);
        var profile = new DefaultAIProfileDefinition().BuildProfiles()[AIProfileType.DroidCompanion];
        var action = profile.Actions.Single(action => action.Feat == FeatType.TwinGuardStance1);

        ability.AITargetSelector.Should().NotBeNull();
        action.TargetSelector.Should().BeSameAs(ability.AITargetSelector);
        ability.AIScore.Should().NotBeNull();
        action.Score.Should().BeSameAs(ability.AIScore);
    }

    [TestCase(FeatType.SacrificialBlade1)]
    [TestCase(FeatType.SoulBurst1)]
    [TestCase(FeatType.SoulStorm1)]
    public void DroidProfile_HitPointSpendingAbilitiesApplyHealthReservesToCustomScores(FeatType feat)
    {
        Ability.CacheData();
        var ability = Ability.GetAbilityDetail(feat);
        ability.AIHitPointCostPercent.Should().NotBeNull();
        // Isolate profile composition from the NWN engine's live Might and hostile-cluster reads.
        // The production cost calculation and score boundaries are exercised in AIModelTests.
        ability.AIHitPointCostPercent = _ => 10;
        ability.AIScore = AIScore.Fixed(123);
        try
        {
            var profile = new DefaultAIProfileDefinition().BuildProfiles()[AIProfileType.DroidCompanion];
            var action = profile.Actions.Single(action => action.Feat == feat);
            var context = new AIContext(100, AITriggerType.CombatRound, 200, profile, new AIState(), []);
            var health = typeof(AIContext).GetField("_selfHitPoints",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            typeof(AIContext).GetField("_selfMaxHitPoints",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(context, 100);
            health.SetValue(context, 59);
            action.Score(context).Should().Be(0);
            health.SetValue(context, 60);
            action.Score(context).Should().Be(123);
        }
        finally
        {
            Ability.CacheData();
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
            FeatType.RendingBite,
            FeatType.CripplingTalons,
            FeatType.ToxicSpit,
            FeatType.PrecisionShot
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
    public void NPCTargetedHostileAreaAbilities_RequireEnemyTargetsForAISelection()
    {
        Ability.CacheData();
        var feats = new[]
        {
            FeatType.ScorchingBreath,
            FeatType.ToxicCloud,
            FeatType.VenomSpray
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
    public void NPCSelfCenteredHostileAreaAbilities_DoNotRequireActivationTargets()
    {
        Ability.CacheData();
        var feats = new[]
        {
            FeatType.RupturingQuake,
            FeatType.SeismicSlam,
            FeatType.DisorientingScreech
        };

        foreach (var feat in feats)
        {
            var ability = Ability.GetAbilityDetail(feat);
            ability.IsHostileAbility.Should().BeTrue($"{feat} affects enemies around the caster");
            ability.IsAreaAbility.Should().BeTrue($"{feat} affects an area");
            ability.RequiresTarget.Should().BeFalse($"{feat} is self-centered and must not request an activation target");
            ability.MaxRange.Should().BeGreaterThan(0f);
        }
    }

    [Test]
    public void PlayerLocationSelectableHostileAreaAbilities_RequireLocationsNotObjects()
    {
        Ability.CacheData();
        var ability = Ability.GetAbilityDetail(FeatType.Provoke2);

        ability.IsHostileAbility.Should().BeTrue();
        ability.IsAreaAbility.Should().BeTrue();
        ability.RequiresTarget.Should().BeFalse();
        ability.RequiresLocationTarget.Should().BeTrue();
    }

    [Test]
    public void NPCSelfBuffAbilities_DoNotRequireTargets()
    {
        Ability.CacheData();
        var ability = Ability.GetAbilityDetail(FeatType.IronCarapace);

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
