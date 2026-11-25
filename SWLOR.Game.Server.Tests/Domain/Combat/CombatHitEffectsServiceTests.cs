using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The hit effects stay-part of <c>Combat</c>: the order in which a direct hit runs the steps of the dealt effects part, the splash, the
/// auto-attack cycles and the forget of the hit effect state of every part that holds some.
/// </summary>
[TestFixture]
public class CombatHitEffectsServiceTests : CombatHitEffectsTestBase
{
    // ------------------------------------------------------------------ auto-attack cycles

    [Test]
    public void TheCycleWithoutARadiusAddsItsDamageOnTheAttackThatCompletesIt()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 3);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 12);
        Stat(Attacker, StatType.AutoAttackCycleDamageSkillType, (int)SkillType.Vibroblade);

        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(12);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "the cycle starts again");
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Rifle).Should().Be(0, "another skill does not advance it");

        _invalid.Add(Gone);
        _service.ConsumeAutoAttackCycleDamageBonus(Gone, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Invalid).Should().Be(0);

        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 5);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "a radius cycle hits a nearby enemy instead");
        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 0);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 12);
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
    }

    [Test]
    public void TheMeleeCycleAddsItsDamageForAnyWeaponSkillAlongsideTheSkillCycle()
    {
        Stat(Attacker, StatType.MeleeAutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 7);

        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(12);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Force).Should().Be(0, "Force is not a weapon skill and opens a new skill cycle");
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(7, "the skill cycle completes; the melee cycle restarted");

        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 3);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(5, "only the melee cycle when the skill cycle has a radius");
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(0);
    }

    [Test]
    public void TheRadiusCycleHitsTheNearestOtherEnemyWithTriggeredDamage()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 9);
        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 4);
        _nearest = Other;
        _queued = true;

        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().BeEmpty("the first attack only counts");
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _log.Should().ContainInOrder(
            $"Nearest {Attacker} {Defender} 4 {Defender}",
            $"Attribute {Attacker} {StatType.AutoAttackCycleDamage}",
            $"TypeDealt {Attacker} 9 {CombatDamageType.Physical}",
            $"Queue {Attacker} {Other} 9 {CombatDamageType.Physical.GetNWScriptDamageType()}");

        _log.Clear();
        _nearest = NWScript.OBJECT_INVALID;
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().ContainSingle();
        Lines("Queue").Should().BeEmpty("no enemy nearby");

        _log.Clear();
        _invalid.Add(Gone);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Vibroblade);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.AutoAttackCycleDamageSkillType, (int)SkillType.Rifle);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().BeEmpty();
    }

    [Test]
    public void TheSourceStatusCycleHitsTheAfflictedTargetAndResetsWhenTheStatusIsGone()
    {
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamage, 6);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, (int)CombatDamageType.Fire);
        _sourceApplied = true;
        _queued = true;

        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _sourceApplied = false;
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _sourceApplied = true;
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        Lines("Queue").Should().BeEmpty("losing the status reset the count");
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _log.Should().ContainInOrder(
            $"Attribute {Attacker} {StatType.SourceStatusAutoAttackCycleDamage}",
            $"TypeDealt {Attacker} 6 {CombatDamageType.Fire}",
            $"Queue {Attacker} {Defender} 6 {CombatDamageType.Fire.GetNWScriptDamageType()}");

        _log.Clear();
        _invalid.Add(Gone);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Katar);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, 0);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        Lines("Queue").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ landed damage


    [Test]
    public void TriggeredAndPeriodicDamageOnlyTrackActivity()
    {
        _directTrait = true;
        Hit(10, delivery: CombatDamageDeliveryType.Triggered);
        Hit(10, delivery: CombatDamageDeliveryType.DamageOverTime);
        _log.Should().Equal($"Activity {Attacker}", $"Recent {Attacker} {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");
    }

    [Test]
    public void ADirectHitRunsItsRidersInOrder()
    {
        _stats[(Attacker, StatType.DamageDealtHPPercentRestore)] = 5;
        _stats[(Attacker, StatType.PhysicalDamageDealtHPPercentRestore)] = 6;

        Hit(10, SkillType.Rifle);
        _log.Should().ContainInOrder(
            $"Beast {Attacker} {Defender}",
            $"Activity {Attacker}",
            $"Recent {Attacker} {Defender}",
            $"FirstAttack {Attacker}",
            $"Pressure {Attacker} {Defender} {SkillType.Rifle}",
            $"AutoSuppression {Attacker} {Defender} {SkillType.Rifle} {CombatDamageType.Physical}",
            $"Consume {Attacker} {StatType.NextDamageDealtBleedDurationSeconds} {StatType.NextDamageDealtBleedDurationSeconds}",
            $"RangedSuppression {Attacker} {Defender} {SkillType.Rifle} {CombatDamageType.Physical}",
            $"Heal {Attacker} 10 5 False 0",
            $"Heal {Attacker} 10 6 False 0");

        _log.Clear();
        Hit(10, SkillType.Force, CombatDamageType.Force, ability: true);
        Lines("AutoSuppression").Should().BeEmpty("an ability does not stack auto-attack Suppression");
        Lines("Heal").Should().Equal($"Heal {Attacker} 10 5 False 0");
    }

    [Test]
    public void AnAbilityImpactAlreadyDamagedThisTargetRunsNoSecondSetOfRiders()
    {
        var weapon = new WeaponShotProfile(501, SkillType.Pistol, 10, CombatDamageType.Physical, 90, 90, AbilityType.Perception, "1");
        var activation = new WeaponImpactActivation();
        using var profile = new WeaponImpactProfile(Attacker, weapon, activation);

        Hit(10, SkillType.Pistol, ability: true);
        Lines("FirstAttack").Should().ContainSingle();
        _log.Clear();
        Hit(10, SkillType.Pistol, ability: true);
        _log.Should().Equal($"Beast {Attacker} {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");
        _log.Clear();
        Hit(10, SkillType.Pistol);
        Lines("FirstAttack").Should().ContainSingle("an auto-attack is not limited by the ability's impact");
    }


    [Test]
    public void AnAutoAttackSplashHitsTheTargetAndItsNeighboursOnTheNextFrame()
    {
        _engine.SetReturn("GetModule", 1u);
        Sources(Attacker, StatType.AutoAttackSplashDamage,
            new Dictionary<StatType, int>
            {
                [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 100,
                [StatType.AutoAttackSplashRadiusMeters] = 4, [StatType.AutoAttackSplashMaximumTargets] = 3
            },
            new Dictionary<StatType, int> { [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 0 });
        _nearby.Add(Other);
        _nearby.Add(Ally);
        _queued = true;

        Hit(10, SkillType.Vibroblade);
        Lines("Queue").Should().BeEmpty("the splash waits a frame");
        var assignment = _engine.CallsTo("AssignCommand").Single();
        assignment.Arguments[0].Should().Be(1u);
        ((Action)assignment.Arguments[1]!)();
        _engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(0f);

        _log.Clear();
        _dead.Add(Ally);
        RunDelayed();
        _log.Should().ContainInOrder(
            $"Attribute {Attacker} {StatType.AutoAttackSplashDamage}",
            $"Queue {Attacker} {Defender} 8 {CombatDamageType.Physical.GetNWScriptDamageType()}",
            $"Queue {Attacker} {Other} 8 {CombatDamageType.Physical.GetNWScriptDamageType()}");
        Lines("Queue").Should().HaveCount(2, "a dead neighbour takes no damage");
        _engine.CallsTo("ApplyEffectAtLocation").Should().ContainSingle();
    }

    [Test]
    public void ASplashSkipsADeadTargetAGoneAttackerAFriendAndAnAbility()
    {
        Sources(Attacker, StatType.AutoAttackSplashDamage, new Dictionary<StatType, int>
        {
            [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 100,
            [StatType.AutoAttackSplashRadiusMeters] = 4, [StatType.AutoAttackSplashMaximumTargets] = 2
        });
        _queued = true;

        Hit(10, SkillType.Vibroblade);
        _dead.Add(Defender);
        _engine.ClearCalls();
        Hit(10, SkillType.Vibroblade);
        RunDelayed();
        Lines("Queue").Should().BeEmpty("the target died before the splash");

        _engine.ClearCalls();
        _dead.Clear();
        Hit(10, SkillType.Vibroblade);
        _invalid.Add(Attacker);
        RunDelayed();
        Lines("Queue").Should().BeEmpty("the attacker is gone");
        _engine.CallsTo("ApplyEffectAtLocation").Should().BeEmpty();

        _invalid.Clear();
        _engine.ClearCalls();
        _hostile = false;
        Hit(10, SkillType.Vibroblade);
        _hostile = true;
        Hit(10, SkillType.Vibroblade, ability: true);
        Hit(10, SkillType.Force);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        _invalid.Add(Defender);
        Hit(10, SkillType.Vibroblade);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }














    [Test]
    public void FrenzySlashRefreshesTheHasteItAlreadyHas()
    {
        Stat(Attacker, StatType.FrenzySlashHasteRefreshDurationSeconds, 6);
        Hit(10);
        Lines("RefreshModifier").Should().Equal($"RefreshModifier {Attacker} {StatType.AttackDelayReductionPercent} 6 {StatType.AttackDelayReductionPercent}");
    }

    // ------------------------------------------------------------------ state

    [Test]
    public void ForgettingACreatureClearsEveryTableThatNamesIt()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.MeleeAutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.CriticalHitSequenceCountRequired, 5);
        Stat(Attacker, StatType.CriticalHitSequenceWindowSeconds, 5);
        Stat(Attacker, StatType.CriticalHitSequenceStaminaRestore, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, (int)CombatDamageType.Fire);
        _sourceApplied = true;
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar);
        Critical();
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Other, 10, SkillType.Katar);
        _autoAttack.ApplyAutoAttackDamageModifiers(Other, Attacker, 10, SkillType.Katar);
        Stat(Other, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);

        IDictionary Table(string name) =>
            (IDictionary)(name == "_criticalHitSequenceStates" ? typeof(CombatCriticalHitEffectsService) : typeof(CombatHitEffectsService))
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(name == "_criticalHitSequenceStates" ? _critical : _service)!;
        Table("_autoAttackCycleCounts").Count.Should().Be(1);
        Table("_meleeAutoAttackCycleCounts").Count.Should().Be(1);
        Table("_criticalHitSequenceStates").Count.Should().Be(1);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(2);

        _service.ForgetHitEffectState(Defender);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(1, "the record naming the defender as the target goes");
        _service.ForgetHitEffectState(Attacker);
        Table("_autoAttackCycleCounts").Count.Should().Be(0);
        Table("_meleeAutoAttackCycleCounts").Count.Should().Be(0);
        Table("_criticalHitSequenceStates").Count.Should().Be(0);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(0);
    }

    [Test]
    public void TheContainerBuildsThePartWithItsInjectedNeighbours()
    {
        _facades.Get<ICombatHitEffectsService>().Should().BeOfType<CombatHitEffectsService>();
    }

    [Test]
    public void TheContainerBuildsTheAutoAttackCriticalAndDefeatedParts()
    {
        _facades.Get<ICombatAutoAttackEffectsService>().Should().BeOfType<CombatAutoAttackEffectsService>();
        _facades.Get<ICombatCriticalHitEffectsService>().Should().BeOfType<CombatCriticalHitEffectsService>();
        _facades.Get<ICombatDefeatedEnemyEffectsService>().Should().BeOfType<CombatDefeatedEnemyEffectsService>();
    }

    [Test]
    public void TheContainerBuildsTheDealtAndTriggeredPartsAndTheBeastRecordIsTheCapstoneProof()
    {
        _facades.Get<ICombatDealtEffectsService>().Should().BeOfType<CombatDealtEffectsService>();
        _facades.Get<ICombatTriggeredDamageService>().Should().BeOfType<CombatTriggeredDamageService>();
        _facades.Get<IBeastDamageRecording>().Should().BeSameAs(_facades.Get<ICapstoneProofService>());
    }
}
