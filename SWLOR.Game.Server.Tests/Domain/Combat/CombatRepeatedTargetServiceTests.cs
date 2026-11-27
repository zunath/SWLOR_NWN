using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The repeated-target and same-target pressure modifiers of <c>Combat</c>: the three repeated-target chains and
/// Spotter's Rhythm, and what forgetting a creature clears.
/// </summary>
[TestFixture]
public class CombatRepeatedTargetServiceTests : CombatModifierTestBase
{
    // ------------------------------------------------------------------ repeated-target chains

    [Test]
    public void TheRepeatedTargetChainStacksPerHitOnOneTargetAndResetsOnAnother()
    {
        Stat(Attacker, StatType.RepeatedTargetDamageSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 10);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentMax, 25);
        Stat(Attacker, StatType.RepeatedTargetDamageBonusPerHit, 2);
        Stat(Attacker, StatType.RepeatedTargetDamageBonusMax, 3);
        Stat(Attacker, StatType.RepeatedTargetDamageDurationSeconds, 5);

        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110 + 2);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(120 + 3);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(125 + 3, "both bonuses are capped");
        Dealt(out _, skill: SkillType.Rifle).Should().Be(100, "a discarded swing neither builds nor breaks the chain");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(125 + 3, "still capped: the stacks stop at the larger cap");

        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(110 + 2, "a new target starts over");
        _now = _now.AddSeconds(6);
        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(110 + 2, "the chain expired");

        Dealt(out _, skill: SkillType.Pistol, landed: true).Should().Be(100, "the chain is the rifle's");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110 + 2, "another skill broke it");

        Stat(Attacker, StatType.RepeatedTargetDamageAutoAttackOnly, 1);
        Dealt(out _, skill: SkillType.Rifle, landed: true, isAbility: true, ability: Hostile()).Should().Be(100);
        Stat(Attacker, StatType.RepeatedTargetDamageAutoAttackOnly, 0);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(100 + 2, "only the flat bonus is left, and the ability broke the chain");
        Stat(Attacker, StatType.RepeatedTargetDamageBonusPerHit, 0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(100);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 10);
        Stat(Attacker, StatType.RepeatedTargetDamageDurationSeconds, 0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110, "a chain with no duration never expires");
        _now = _now.AddHours(1);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(120);

        _invalid.Add(Gone);
        _service.ApplyDamageDealtModifiers(Attacker, Gone, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(100);
        _service.ApplyDamageDealtModifiers(Attacker, Attacker, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(100, "a creature cannot chain on itself");
    }

    [Test]
    public void TheMeleeChainStacksOnAutoAttacksShowsItsStacksAndClearsWhenUnconfigured()
    {
        Stat(Attacker, StatType.MeleeRepeatedTargetDamageBonusPerHit, 2);
        Stat(Attacker, StatType.MeleeRepeatedTargetDamageBonusMax, 5);
        Stat(Attacker, StatType.MeleeRepeatedTargetDamageStatusEffectIcon, (int)EffectIconType.Haste);

        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(102);
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(104);
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(105, "capped at 5");
        Dealt(out _, skill: SkillType.Vibroblade, landed: true, isAbility: true, ability: Hostile(SkillType.Vibroblade))
            .Should().Be(100, "abilities neither build nor break the chain");
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(105);
        _applied.OfType<MeleeRepeatedTargetDamageStatusEffect>().Select(effect => effect.Stacks).Should().Equal(1, 2, 3, 3);

        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Vibroblade, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(102, "a new target starts over");

        _applied.Clear();
        Dealt(out _, skill: SkillType.Force, landed: true).Should().Be(100, "not a weapon");
        _applied.OfType<MeleeRepeatedTargetDamageStatusEffect>().Should().BeEmpty("the chain's status is cleared");
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(102, "the chain was cleared");
    }

    [Test]
    public void SustainedFireStacksOnWeaponHitsShowsTheBonusAndLetsOtherDamagePassUntouched()
    {
        _players.Add(Attacker);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusPerHit, 3);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusMax, 7);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageDurationSeconds, 4);

        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103);
        Dealt(out _, skill: SkillType.Rifle, landed: true, isAbility: true, ability: Hostile()).Should().Be(106, "ability hits count too");
        Dealt(out _, skill: SkillType.Force, landed: true).Should().Be(100, "non-weapon damage leaves the stack untouched");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(107, "capped at 7");
        _applied.OfType<SustainedFireStatusEffect>().Select(effect => $"{effect.Stacks}/{effect.MaximumStacks} +{effect.DamageBonus}")
            .Should().Equal("1/3 +3", "2/3 +6", "3/3 +7");
        FloatingText().Should().HaveCount(3).And.Contain(text => text.Contains("Sustained Fire 3/3 (+7 DMG)"));
        _statusLog.Should().Contain($"Apply {Attacker}>{Attacker} {nameof(SustainedFireStatusEffect)} 4");

        _now = _now.AddSeconds(5);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103, "the stack expired");
        _players.Remove(Attacker);
        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(103, "a new target starts over");
        FloatingText().Should().HaveCount(4, "an NPC sees no text");

        Stat(Attacker, StatType.RangedRepeatedTargetDamageDurationSeconds, 0);
        _statusLog.Clear();
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(100);
        _statusLog.Should().Contain($"Remove {Attacker} {nameof(SustainedFireStatusEffect)}");
    }

    [Test]
    public void ClearingTheTrackersRemovesTheirStacksAndStatuses()
    {
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusPerHit, 3);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusMax, 9);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageDurationSeconds, 4);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(106);

        _statusLog.Clear();
        _repeated.ClearRangedRepeatedTargetDamageTracker(Attacker);
        _statusLog.Should().Equal($"Remove {Attacker} {nameof(SustainedFireStatusEffect)}");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103, "the stack starts over");

        _statusLog.Clear();
        _rate.ClearAutoAttackCycleCriticalRateTracker(Attacker);
        _statusLog.Should().Equal($"Remove {Attacker} {nameof(AttackCycleTrackerStatusEffect)}");
    }

    // ------------------------------------------------------------------ Spotter's Rhythm

    private void ConfigurePressure(int buildSeconds = 6, int graceSeconds = 2, int readySeconds = 10, int bonus = 8)
    {
        Stat(Attacker, StatType.SameTargetPressureBuildSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.SameTargetPressureBuildSeconds, buildSeconds);
        Stat(Attacker, StatType.SameTargetPressureGraceSeconds, graceSeconds);
        Stat(Attacker, StatType.SameTargetPressureReadyDurationSeconds, readySeconds);
        Stat(Attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus, bonus);
    }

    private void Hit(uint target, SkillType skill = SkillType.Rifle, int seconds = 1)
    {
        _repeated.ApplySameTargetPressureDamageEffects(Attacker, target, skill);
        _now = _now.AddSeconds(seconds);
    }

    [Test]
    public void SteadyHitsOnOneTargetReadySpottersRhythmForTheNextWeaponAbility()
    {
        _players.Add(Attacker);
        ConfigurePressure();
        for (var i = 0; i < 6; i++)
            Hit(Defender);
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0, "6 seconds have not passed at the last hit");
        Hit(Defender);
        _statusLog.Should().Equal($"Apply {Attacker}>{Attacker} {nameof(SpottersRhythmStatusEffect)} 10");
        FloatingText().Should().ContainSingle().Which.Should().Contain("Spotter's Rhythm");

        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8);
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Force).Should().Be(0, "weapon abilities only");
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Other, SkillType.Rifle).Should().Be(0, "another target");
        Hit(Other, seconds: 0);
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8, "a ready state is kept while active");

        _repeated.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 0);
        _repeated.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Other, SkillType.Rifle, 50);
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8, "nothing was consumed");
        _repeated.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 50);
        _statusLog.Last().Should().Be($"Remove {Attacker} {nameof(SpottersRhythmStatusEffect)} from {Attacker}");
        FloatingText().Last().Should().Contain("Spotter's Rhythm (+8 DMG)");
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0);
    }

    [Test]
    public void SpottersRhythmBuildsOnlyFromItsSkillResetsAfterTheGraceAndExpiresWithItsStatus()
    {
        ConfigurePressure();
        Hit(Defender, SkillType.Force);         // 0: not a weapon
        Hit(Defender, SkillType.Pistol);        // 1: not the build skill
        Hit(Defender, seconds: 3);              // 2: the build starts
        Hit(Defender);                          // 5: three seconds is past the grace, so it starts over
        Hit(Defender, seconds: 2);              // 6
        Hit(Defender, seconds: 2);              // 8
        Hit(Defender);                          // 10
        _statusLog.Should().BeEmpty("the build restarted at 5 seconds");
        Hit(Defender, seconds: 0);              // 11: six seconds since the restart
        _statusLog.Should().ContainSingle(line => line.StartsWith("Apply"));

        Hit(Defender, SkillType.Pistol, 0);
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8);
        _statuses.Remove((Attacker, typeof(SpottersRhythmStatusEffect)));
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0, "the status was removed");
        _statusLog.Last().Should().Be($"Remove {Attacker} {nameof(SpottersRhythmStatusEffect)} from {Attacker}");

        for (var i = 0; i < 3; i++)
            Hit(Defender);                      // 11 to 13: a new build starts at 11
        Hit(Other, SkillType.Pistol, 0);        // 14: a pistol hit on another target clears it
        for (var i = 0; i < 6; i++)
            Hit(Defender);                      // 14 to 19: the build starts again at 14
        _statusLog.Count(line => line.StartsWith("Apply")).Should().Be(1, "without the reset it would have readied at 17");
        Hit(Defender);                          // 20
        _statusLog.Count(line => line.StartsWith("Apply")).Should().Be(2);

        _now = _now.AddSeconds(11);
        Hit(Defender, seconds: 0);
        _statusLog.Last().Should().StartWith("Remove", "an expired ready state is cleared before building again");
        _repeated.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 50);
        FloatingText().Should().BeEmpty("an NPC sees no text");
    }

    [Test]
    public void SpottersRhythmNeedsEveryStatAndAValidPairOfCreatures()
    {
        ConfigurePressure(bonus: 0);
        for (var i = 0; i < 8; i++)
            Hit(Defender);
        _invalid.Add(Gone);
        ConfigurePressure(buildSeconds: 1);
        Hit(Gone);
        Hit(Gone);
        Hit(Attacker);
        Hit(Attacker);
        _repeated.ApplySameTargetPressureDamageEffects(Gone, Defender, SkillType.Rifle);
        _statusLog.Should().BeEmpty();
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0);
    }

    [Test]
    public void ForgettingACreatureClearsItsStacksAndTheRhythmsTargetingIt()
    {
        ConfigurePressure(buildSeconds: 1);
        Hit(Defender);
        Hit(Defender);
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8);
        Stat(Attacker, StatType.RepeatedTargetDamageSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 10);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentMax, 50);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110);

        _statusLog.Clear();
        _repeated.ForgetDamageModifierState(Defender);
        _statusLog.Should().Equal(
            $"Remove {Defender} {nameof(SpottersRhythmStatusEffect)} from {Defender}",
            $"Remove {Attacker} {nameof(SpottersRhythmStatusEffect)} from {Attacker}");
        _repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(120, "the attacker's own chain is kept");

        _repeated.ForgetDamageModifierState(Attacker);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110, "forgotten");
    }
}
