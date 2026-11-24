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
/// The defense effects part of <c>Combat</c>: the evasion, the attack and ranged deflection, the nearby ally defense and the
/// perk category guard that an activation grants.
/// </summary>
[TestFixture]
public class CombatAbilityDefenseEffectsServiceTests : CombatAbilityActivationTestBase
{
    [Test]
    public void TheSkillEvasionRunsBothChannelsAndAppliesFootworkWhereTheTriggerSkillMatches()
    {
        _abilitySkill = SkillType.Rifle;
        _matchingTriggerSkills.Add((StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType, StatType.SecondaryAbilityUsedEvasionRangedOnly));
        Stat(Activator, StatType.AbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.AbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionPercentAdjustment, 12);
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionDurationSeconds, 4);

        _defenseEffects.ApplyAbilityUsedSkillEvasion(Activator, Hostile());

        _asked.Should().Equal(
            $"TriggerSkill {Activator} {SkillType.Rifle} {StatType.AbilityUsedEvasionPercentAdjustmentSkillType} null",
            $"TriggerSkill {Activator} {SkillType.Rifle} {StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType} {StatType.SecondaryAbilityUsedEvasionRangedOnly}");
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveFootworkStatusEffect(12) 4");

        _log.Clear();
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionDurationSeconds, 0);
        _defenseEffects.ApplyAbilityUsedSkillEvasion(Activator, Hostile());
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionDurationSeconds, 4);
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionPercentAdjustment, 0);
        _defenseEffects.ApplyAbilityUsedSkillEvasion(Activator, Hostile());
        _log.Should().BeEmpty("no duration, then no evasion");
    }

    [Test]
    public void TheHostileAndCostlyEvasionNeedAHostileAbilityAMatchingSkillAndTheirStats()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        _defenseEffects.ApplyHostileAbilityUsedEvasion(Activator, ability, SkillType.Pistol);
        _log.Should().BeEmpty("the skill does not match");

        _defenseEffects.ApplyHostileAbilityUsedEvasion(Activator, new AbilityDetail { Name = "Heal" }, SkillType.Rifle);
        _log.Should().BeEmpty("not hostile");

        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 0);
        _defenseEffects.ApplyHostileAbilityUsedEvasion(Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 0);
        _defenseEffects.ApplyHostileAbilityUsedEvasion(Activator, ability, SkillType.Rifle);
        _log.Should().BeEmpty("no duration, then no evasion");

        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, -4);
        _defenseEffects.ApplyHostileAbilityUsedEvasion(Activator, ability, SkillType.Rifle);
        _log.Should().Equal($"Replace {Activator} {StatType.EvasionPercentAdjustment} -4 5 {StatType.HostileAbilityUsedEvasionPercentAdjustment}");

        // The costly evasion needs the stamina this activation paid, at least the minimum, and a matching skill.
        _log.Clear();
        Stat(Activator, StatType.CostlyAbilityUsedEvasionPercentAdjustment, 8);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionDurationSeconds, 4);
        _defenseEffects.ApplyCostlyAbilityUsedEvasion(Activator, ability, SkillType.Rifle);
        _staminaCost = 10;
        _defenseEffects.ApplyCostlyAbilityUsedEvasion(Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionMinimumStaminaCost, 15);
        _defenseEffects.ApplyCostlyAbilityUsedEvasion(Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Pistol);
        _staminaCost = 15;
        _defenseEffects.ApplyCostlyAbilityUsedEvasion(Activator, ability, SkillType.Rifle);
        _defenseEffects.ApplyCostlyAbilityUsedEvasion(Activator, new AbilityDetail { Name = "Heal" }, SkillType.Pistol);
        _log.Should().BeEmpty("no paid stamina, no minimum, below the minimum, another skill, then not hostile");
        _asked.Count(line => line.StartsWith("StaminaCost", StringComparison.Ordinal)).Should().Be(4, "a non-hostile ability never asks");

        _defenseEffects.ApplyCostlyAbilityUsedEvasion(Activator, ability, SkillType.Pistol);
        _log.Should().Equal($"Replace {Activator} {StatType.EvasionPercentAdjustment} 8 4 {StatType.CostlyAbilityUsedEvasionPercentAdjustment}");
    }

    [Test]
    public void TheAreaEvasionNeedsAHostileAreaAbilityAMatchingSkillAndItsStats()
    {
        var ability = Hostile();
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 9);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 3);

        _defenseEffects.ApplyAreaAbilityUsedEvasion(Activator, ability, SkillType.Rifle, false);
        _defenseEffects.ApplyAreaAbilityUsedEvasion(Activator, new AbilityDetail { Name = "Heal" }, SkillType.Rifle, true);
        _defenseEffects.ApplyAreaAbilityUsedEvasion(Activator, ability, SkillType.Pistol, true);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 0);
        _defenseEffects.ApplyAreaAbilityUsedEvasion(Activator, ability, SkillType.Rifle, true);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 3);
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 0);
        _defenseEffects.ApplyAreaAbilityUsedEvasion(Activator, ability, SkillType.Rifle, true);
        _log.Should().BeEmpty("not an area ability, not hostile, another skill, no duration, then no evasion");

        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 9);
        _defenseEffects.ApplyAreaAbilityUsedEvasion(Activator, ability, SkillType.Rifle, true);
        _log.Should().Equal($"Replace {Activator} {StatType.EvasionPercentAdjustment} 9 3 {StatType.AreaAbilityUsedEvasionPercentAdjustment}");
    }

    [Test]
    public void TheRangedDeflectionNeedsAHostileAbilityAMatchingTriggerItsStatsAndAnIconAndRunsItsRidersOnlyWhenApplied()
    {
        var ability = Hostile();
        _deflectionSources[StatType.AbilityUsedRangedDeflection] = DeflectionSource.Ranged;
        Stat(Activator, StatType.AbilityUsedRangedDeflection, 10);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.DeflectingAuraStatusEffect);

        _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(Activator, ability);
        _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("no matching trigger, then not hostile");

        _matchingTriggerSkills.Add((StatType.AbilityUsedRangedDeflectionSkillType, StatType.AbilityUsedRangedDeflectionRangedOnly));
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, 0);
        _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(Activator, ability);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.DeflectingAuraStatusEffect);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 0);
        _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(Activator, ability);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedRangedDeflection, 0);
        _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(Activator, ability);
        _log.Should().BeEmpty("no icon, no duration, then no deflection");

        Stat(Activator, StatType.AbilityUsedRangedDeflection, 10);
        _statusApplies = false;
        _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(Activator, ability);
        _log.Should().Equal(
            new[] { $"Apply {Activator}>{Activator} RangedDeflectionStatusEffect(10,{EffectIconType.DeflectingAuraStatusEffect}) 5" },
            "a status that was not applied runs no deflection riders");

        _log.Clear();
        _statusApplies = true;
        _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(Activator, ability);
        _log.Should().Equal(
            $"Apply {Activator}>{Activator} RangedDeflectionStatusEffect(10,{EffectIconType.DeflectingAuraStatusEffect}) 5",
            $"GrantedDeflection {Activator} {DeflectionSource.Ranged}");
    }

    [Test]
    public void TheAttackDeflectionRidersNeedTheirSkillOrCategoryAndPassTheirOwnStats()
    {
        var ability = Hostile();
        ability.EffectiveLevelPerkType = PerkType.DoubleShot;
        _abilitySkill = SkillType.Pistol;
        Stat(Activator, StatType.AbilityUsedAttackDeflectionSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.SingleTargetAbilityAttackDeflectionSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType, 3);

        _defenseEffects.ApplyAbilityUsedSkillAttackDeflection(Activator, ability);
        _defenseEffects.ApplySingleTargetAbilityUsedAttackDeflection(Activator, ability, true);
        _defenseEffects.ApplyAbilityUsedPerkCategoryAttackDeflection(Activator, ability);
        _log.Should().BeEmpty("another skill and a perk outside the category");
        _asked.Should().Contain($"InCategory {PerkType.DoubleShot} 3");

        _abilitySkill = SkillType.Rifle;
        _perkCategories.Add(3);
        _defenseEffects.ApplyAbilityUsedSkillAttackDeflection(Activator, new AbilityDetail { Name = "Heal" });
        _defenseEffects.ApplySingleTargetAbilityUsedAttackDeflection(Activator, ability, false);
        _log.Should().BeEmpty("not hostile, then not single target");

        _defenseEffects.ApplyAbilityUsedSkillAttackDeflection(Activator, ability);
        _defenseEffects.ApplySingleTargetAbilityUsedAttackDeflection(Activator, ability, true);
        _defenseEffects.ApplyAbilityUsedPerkCategoryAttackDeflection(Activator, ability);
        _log.Should().Equal(
            $"UsedDeflection {Activator} {StatType.AbilityUsedAttackDeflection} {StatType.AbilityUsedAttackDeflectionDurationSeconds} {StatType.AbilityUsedAttackDeflectionFPRestore}",
            $"UsedDeflection {Activator} {StatType.SingleTargetAbilityAttackDeflection} {StatType.SingleTargetAbilityAttackDeflectionDurationSeconds} {StatType.Invalid}",
            $"UsedDeflection {Activator} {StatType.AbilityUsedPerkCategoryAttackDeflection} {StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds} {StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore}");
    }

    [Test]
    public void APerkCategoryDeflectionOfAnAbilityWithNoDetailAsksForNoPerk()
    {
        _perkCategories.Add(0);

        _defenseEffects.ApplyAbilityUsedPerkCategoryAttackDeflection(Activator, null);

        _asked.Should().Equal($"InCategory {PerkType.Invalid} 0");
        _log.Should().ContainSingle().Which.Should().StartWith($"UsedDeflection {Activator} {StatType.AbilityUsedPerkCategoryAttackDeflection}");
    }

    [Test]
    public void TheNearbyAllyDefenseGoesToPartyMembersInRangeButNotTheActivator()
    {
        _inShape.AddRange(new[] { Activator, Ally, Bystander });
        _party.Add(Ally);
        Stat(Activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment, 5);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 6);

        _defenseEffects.ApplyAbilityUsedNearbyAllyDefense(Activator);
        _log.Should().Equal($"Replace {Ally} {StatType.ForceDefensePercentAdjustment} 5 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment, 4);
        _defenseEffects.ApplyAbilityUsedNearbyAllyDefense(Activator);
        _log.Should().Equal(
            $"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 4 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}",
            $"Replace {Ally} {StatType.ForceDefensePercentAdjustment} 5 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment, 0);
        _defenseEffects.ApplyAbilityUsedNearbyAllyDefense(Activator);
        _log.Should().Equal($"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 4 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 0);
        _defenseEffects.ApplyAbilityUsedNearbyAllyDefense(Activator);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 6);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment, 0);
        _defenseEffects.ApplyAbilityUsedNearbyAllyDefense(Activator);
        _log.Should().BeEmpty("no duration, then no defense of either kind");
    }

    [Test]
    public void TheNearbyAllyAttackDeflectionNeedsTheCategoryAGrantedTypeADeflectionAndADuration()
    {
        var ability = Hostile();
        _inShape.AddRange(new[] { Ally, Activator });
        _party.Add(Ally);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 6);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment, 25);

        _defenseEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(Activator, ability);
        _log.Should().BeEmpty("the category does not match");

        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId);
        _defenseEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(Activator, ability);
        _log.Should().BeEmpty("no granted deflection type");

        _grantedDeflections[StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection] = StatType.RangedDeflection;
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 0);
        _defenseEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(Activator, ability);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 0);
        _defenseEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(Activator, ability);
        _log.Should().BeEmpty("no duration, then no deflection");

        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 6);
        _defenseEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"Apply {Activator}>{Ally} SentinelGuardStatusEffect(6,25,{StatType.RangedDeflection}) 5",
                $"Apply {Activator}>{Activator} SentinelGuardStatusEffect(6,25,{StatType.RangedDeflection}) 5",
            },
            "the activator is one of the friends this search returns");
    }

    [Test]
    public void TheSelfDefenseNeedsTheCategoryADurationOneDefenseAndAReadyTrigger()
    {
        var ability = Hostile();
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment, 4);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment, 2);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds, 30);

        _defenseEffects.ApplyAbilityUsedPerkCategorySelfDefense(Activator, ability);
        _log.Should().BeEmpty("the category does not match");

        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategorySelfDefenseCategoryId);
        _triggerReady = false;
        _defenseEffects.ApplyAbilityUsedPerkCategorySelfDefense(Activator, ability);
        _log.Should().Equal($"Trigger {Activator} {StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment} 30");

        _log.Clear();
        _triggerReady = true;
        _defenseEffects.ApplyAbilityUsedPerkCategorySelfDefense(Activator, ability);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment} 30",
            $"Apply {Activator}>{Activator} GuardingStepStatusEffect(0,4,2) 5");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment, 0);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment, 0);
        _defenseEffects.ApplyAbilityUsedPerkCategorySelfDefense(Activator, ability);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment, 3);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds, 0);
        _defenseEffects.ApplyAbilityUsedPerkCategorySelfDefense(Activator, ability);
        _log.Should().BeEmpty("no defense of any kind, then no duration");
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheConstructorOnlyAssignsAndTheContainerBuildsOneInstanceWithTheConvertedParts()
    {
        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        _engine.Calls.Should().BeEmpty("the constructor makes no engine call");

        var part = _facades.Get<ICombatAbilityDefenseEffectsService>();
        part.Should().BeOfType<CombatAbilityDefenseEffectsService>();
        _facades.Get<ICombatAbilityDefenseEffectsService>().Should().BeSameAs(part);
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CombatAbilityDefenseEffectsService).GetField("_damageTakenEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatDamageTakenEffectsService>());
        typeof(CombatAbilityDefenseEffectsService).GetField("_perk", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<IPerkService>());
        typeof(CombatAbilityDefenseEffectsService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().BeEmpty("the part holds no static state");
        typeof(CombatAbilityDefenseEffectsService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().OnlyContain(field => field.IsInitOnly, "the part holds no state of its own");
    }
}
