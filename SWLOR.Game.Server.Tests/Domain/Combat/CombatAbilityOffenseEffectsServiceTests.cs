using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The offense effects part of <c>Combat</c>: the movement speed, the area ability rewards, the hostile ability Force Attack,
/// attack adjustment, sequence and resource restore riders, the next auto-attack damage bonus and the part's own hostile
/// ability sequence table.
/// </summary>
[TestFixture]
public class CombatAbilityOffenseEffectsServiceTests : CombatAbilityActivationTestBase
{
    [Test]
    public void TheMovementSpeedNeedsAHostileAbilityAMatchingTriggerAndItsStats()
    {
        var ability = Hostile();
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 15);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        Private(_offenseEffects, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        Private(_offenseEffects, "ApplyAbilityUsedMovementSpeed", Activator, new AbilityDetail { Name = "Heal" }, SkillType.Rifle);
        _log.Should().BeEmpty("no matching trigger, then not hostile");
        _asked.Should().ContainSingle().Which.Should().Be(
            $"TriggerSkill {Activator} {SkillType.Rifle} {StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType} {StatType.AbilityUsedMovementSpeedRangedOnly}");

        _matchingTriggerSkills.Add((StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType, StatType.AbilityUsedMovementSpeedRangedOnly));
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 0);
        Private(_offenseEffects, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 0);
        Private(_offenseEffects, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        _log.Should().BeEmpty("no duration, then no speed");

        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, -10);
        Private(_offenseEffects, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        _log.Should().Equal($"Replace {Activator} {StatType.MovementSpeedPercentAdjustment} -10 4 {StatType.AbilityUsedMovementSpeedPercentAdjustment}");
    }

    [Test]
    public void TheAreaRewardsGrantEachConfiguredSourcesDeflectionThenTheRidersOnceThenTheFPRestore()
    {
        var ability = Hostile();
        _grantedDeflections[StatType.AreaAbilityUsedAttackDeflection] = StatType.ShieldDeflection;
        _deflectionSources[StatType.AreaAbilityUsedAttackDeflection] = DeflectionSource.Shield;
        Sources(Activator, StatType.AreaAbilityUsedAttackDeflection,
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 5, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 4 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 0, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 4 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 3, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 0 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 7, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 6 });
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 3);

        Private(_offenseEffects, "ApplyAreaAbilityUsedRewards", Activator, ability, false);
        Private(_offenseEffects, "ApplyAreaAbilityUsedRewards", Activator, new AbilityDetail { Name = "Heal" }, true);
        _log.Should().BeEmpty("not an area ability, then not hostile");

        Private(_offenseEffects, "ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().Equal(
            new[]
            {
                $"Replace {Activator} {StatType.ShieldDeflection} 5 4 source0:{(int)StatType.AreaAbilityUsedAttackDeflection}",
                $"Replace {Activator} {StatType.ShieldDeflection} 7 6 source3:{(int)StatType.AreaAbilityUsedAttackDeflection}",
                $"GrantedDeflection {Activator} {DeflectionSource.Shield}",
                $"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 3",
            },
            "nothing was restored, so the restored FP riders do not run");

        _log.Clear();
        _restoredFP = 3;
        _sources.Clear();
        Private(_offenseEffects, "ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().Equal($"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 3", $"RestoredFP {Activator}");

        _log.Clear();
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 0);
        Private(_offenseEffects, "ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().BeEmpty("no restore and no deflection source");
    }

    [Test]
    public void TheHostileForceAttackStacksOnTheCurrentStatusUpToItsMaximum()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 6);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 15);
        _currentForceAttack = new HostileAbilityForceAttackStatusEffect(12);

        Private(_offenseEffects, "ApplyHostileAbilityForceAttack", Activator, ability);

        _asked.Should().Equal($"Status {Activator} {nameof(HostileAbilityForceAttackStatusEffect)}");
        _log.Should().Equal($"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(15) 10");

        _log.Clear();
        _currentForceAttack = null;
        Private(_offenseEffects, "ApplyHostileAbilityForceAttack", Activator, ability);
        _log.Should().Equal($"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(6) 10");
    }

    [Test]
    public void TheHostileForceAttackNeedsAHostileAbilityAndAllThreeStats()
    {
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 6);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 15);
        Private(_offenseEffects, "ApplyHostileAbilityForceAttack", Activator, new AbilityDetail { Name = "Heal" });
        foreach (var stat in new[] { StatType.HostileAbilityForceAttackPercentMax, StatType.HostileAbilityForceAttackDurationSeconds, StatType.HostileAbilityForceAttackPercentPerStack })
        {
            Stat(Activator, stat, 0);
            Private(_offenseEffects, "ApplyHostileAbilityForceAttack", Activator, Hostile());
        }

        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
    }

    [Test]
    public void TheFPSpendForceAttackNeedsAHostileAbilityItsStatsAndAnFPCostAtTheMinimum()
    {
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackPercent, 3);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 9);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent, 9);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost, 8);

        var twoCosts = Hostile(fp: 4);
        twoCosts.Requirements.Add(new AbilityRequirementFP(4));
        Private(_offenseEffects, "ApplyHostileAbilityFPSpendForceAttack", Activator, Hostile(fp: 7));
        Private(_offenseEffects, "ApplyHostileAbilityFPSpendForceAttack", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("below the minimum, then not hostile");

        Private(_offenseEffects, "ApplyHostileAbilityFPSpendForceAttack", Activator, twoCosts);
        _log.Should().Equal(
            new[] { $"AddCapped {Activator} {StatType.ForceAttackPercentAdjustment} 3 9 9 {StatType.HostileAbilityFPSpendForceAttackPercent} 1 True" },
            "the FP requirements add up");

        _log.Clear();
        foreach (var stat in new[] { StatType.HostileAbilityFPSpendForceAttackMinFPCost, StatType.HostileAbilityFPSpendForceAttackMaxPercent, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, StatType.HostileAbilityFPSpendForceAttackPercent })
        {
            Stat(Activator, stat, 0);
            Private(_offenseEffects, "ApplyHostileAbilityFPSpendForceAttack", Activator, twoCosts);
        }

        _log.Should().BeEmpty();
    }

    [Test]
    public void TheAttackAdjustmentStacksAndShowsTheTempoOnlyWhileItsTotalIsPositive()
    {
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustment, 2);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, 8);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, 10);

        Private(_offenseEffects, "ApplyHostileAbilityUsedAttackAdjustment", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty();

        Private(_offenseEffects, "ApplyHostileAbilityUsedAttackAdjustment", Activator, Hostile());
        _asked.Should().Equal($"Temporary {Activator} {StatType.AttackPercentAdjustment} {StatType.HostileAbilityUsedAttackPercentAdjustment}");
        _log.Should().Equal(
            new[] { $"AddCapped {Activator} {StatType.AttackPercentAdjustment} 2 8 10 {StatType.HostileAbilityUsedAttackPercentAdjustment} 1 True" },
            "no stacks are held, so no tempo status");

        _log.Clear();
        foreach (var stat in new[] { StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, StatType.HostileAbilityUsedAttackPercentAdjustment })
        {
            Stat(Activator, stat, 0);
            Private(_offenseEffects, "ApplyHostileAbilityUsedAttackAdjustment", Activator, Hostile());
        }

        _log.Should().BeEmpty();
    }

    [Test]
    public void ADifferentHostileAbilityWithinTheWindowArmsTheNextAttackBleed()
    {
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);

        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("the first ability starts the sequence");

        _now = _now.AddSeconds(6);
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("the same ability again does not continue the sequence");

        _now = _now.AddSeconds(6);
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.PowerAttack, Hostile());
        _log.Should().Equal(
            new[] { $"Replace {Activator} {StatType.NextDamageDealtBleedDurationSeconds} 3 6 {StatType.NextDamageDealtBleedDurationSeconds}" },
            "a different ability at the edge of the window continues it");

        _log.Clear();
        _now = _now.AddSeconds(6.5);
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("past the window");
    }

    [Test]
    public void TheSequenceRecordsTheLastHostileAbilityEvenWithoutABleedAndIgnoresAnythingElse()
    {
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Sequences().Contains(Activator).Should().BeTrue();

        // With the bleed configured now, the ability recorded without it continues the sequence.
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);
        _now = _now.AddSeconds(2);
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.PowerAttack, Hostile());
        _log.Should().ContainSingle();

        // A window with no bleed records the use and arms nothing.
        _log.Clear();
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 0);
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty();

        // A non-hostile ability, a missing ability and an invalid activator are not recorded.
        Sequences().Clear();
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, new AbilityDetail { Name = "Heal" });
        typeof(CombatAbilityOffenseEffectsService).GetMethod("ApplyHostileAbilitySequenceEffects", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_offenseEffects, new object?[] { Activator, FeatType.DoubleShot1, null });
        _invalid.Add(Activator);
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Sequences().Count.Should().Be(0);
    }

    [Test]
    public void ForgettingACreatureDropsOnlyItsSequence()
    {
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Private(_offenseEffects, "ApplyHostileAbilitySequenceEffects", Ally, FeatType.DoubleShot1, Hostile());

        _offenseEffects.ForgetActivationState(Activator);
        _offenseEffects.ForgetActivationState(Bystander);

        Sequences().Contains(Activator).Should().BeFalse();
        Sequences().Contains(Ally).Should().BeTrue();
    }

    [Test]
    public void TheHostileResourceRestoresRunTheirRidersOnlyForWhatWasRestored()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityFPRestore, 3);
        Stat(Activator, StatType.HostileAbilityStaminaRestore, 4);

        Private(_offenseEffects, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
                $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4",
            },
            "no FP was restored, so neither the FP nor the both-resources riders run");

        _log.Clear();
        _restoredFP = 3;
        Private(_offenseEffects, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
            $"RestoredFP {Activator}",
            $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4",
            $"RestoredBoth {Activator}");

        _log.Clear();
        _restoredStamina = 0;
        Private(_offenseEffects, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
            $"RestoredFP {Activator}",
            $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4");

        _log.Clear();
        Stat(Activator, StatType.HostileAbilityFPRestore, 0);
        Stat(Activator, StatType.HostileAbilityStaminaRestore, 0);
        Private(_offenseEffects, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        Stat(Activator, StatType.HostileAbilityFPRestore, 3);
        Private(_offenseEffects, "ApplyHostileAbilityResourceRestoreEffects", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("nothing to restore, then not hostile");
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheConstructorOnlyAssignsAndTheContainerBuildsOneInstanceWithTheConvertedParts()
    {
        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        _engine.Calls.Should().BeEmpty("the constructor makes no engine call");

        var part = _facades.Get<ICombatAbilityOffenseEffectsService>();
        part.Should().BeOfType<CombatAbilityOffenseEffectsService>();
        _facades.Get<ICombatAbilityOffenseEffectsService>().Should().BeSameAs(part);
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CombatAbilityOffenseEffectsService).GetField("_damageTakenEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatDamageTakenEffectsService>());
        typeof(CombatAbilityOffenseEffectsService).GetField("_statusEffect", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<IStatusEffectService>());
        typeof(CombatAbilityOffenseEffectsService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().BeEmpty("the part holds no static state");
        typeof(CombatAbilityOffenseEffectsService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(field => typeof(System.Collections.IDictionary).IsAssignableFrom(field.FieldType)).Select(field => field.Name)
            .Should().ContainSingle("the hostile ability sequence table is the part's only state").Which.Should().Be("_hostileAbilitySequenceStates");
    }
}
