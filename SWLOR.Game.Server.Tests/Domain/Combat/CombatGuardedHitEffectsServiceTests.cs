using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The effects a guarded hit grants the guard: the remembered hit, the next attack and next skill ability counters and the Guard Reversal rider.
/// </summary>
[TestFixture]
public class CombatGuardedHitEffectsServiceTests : CombatDamageTakenTestBase
{
    [Test]
    public void TrackingAGuardedHitRecordsItAndArmsTheCounterRiders()
    {
        _guardedEffects.TrackGuardedHit(Defender);
        GuardedHits()[Defender].Should().Be(_now);
        _log.Should().Equal($"NextSkill {Defender} Invalid 0 0 0 0 None");

        _log.Clear();
        _invalid.Add(Gone);
        _guardedEffects.TrackGuardedHit(Gone);
        _log.Should().BeEmpty();
        GuardedHits().Should().NotContainKey(Gone);
    }

    [Test]
    public void ACounterOfOneSkillStacksBothGuardedHitSourcesAndTellsAPlayer()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityDamageBonus, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 6);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus, 3);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityCriticalRatePercentAdjustment, 2);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds, 9);

        _guardedEffects.TrackGuardedHit(Defender);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} Vibroblade 7 7 9 0 None");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Counter Ready: +7 DMG, +7% Crit")}");
    }

    [Test]
    public void ACounterWithOnlyTheSecondarySourceUsesItAndOmitsAZeroCritical()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilitySkillType, (int)SkillType.Spear);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus, 3);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds, 9);

        _guardedEffects.TrackGuardedHit(Defender);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} Spear 3 0 9 0 None");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Counter Ready: +3 DMG")}");
    }

    [TestCase(SkillType.Spear, "Spear 3 0 9")]
    [TestCase(SkillType.Vibroblade, "Vibroblade 4 0 6")]
    public void CountersOfTwoSkillsPreferTheEquippedWeaponsAndOtherwiseThePrimary(SkillType equipped, string expected)
    {
        _equippedSkill = equipped;
        Stat(Defender, StatType.GuardedHitNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityDamageBonus, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 6);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilitySkillType, (int)SkillType.Spear);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus, 3);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds, 9);

        _guardedEffects.TrackGuardedHit(Defender);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} {expected} 0 None");
        FloatingText().Should().BeEmpty("the defender is not a player");
    }

    [Test]
    public void ACounterWithoutAWindowOrBonusShowsNoText()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 6);

        _guardedEffects.TrackGuardedHit(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityDamageBonus, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 0);
        _guardedEffects.TrackGuardedHit(Defender);
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void GuardReversalArmsTheExposedRiderForItsWindow()
    {
        Stat(Defender, StatType.GuardedHitNextSkillAbilityStatusSkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 6);
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDamageBonus] = 6;

        _guardedEffects.TrackGuardedHit(Defender);
        _log.Skip(1).Should().Equal(
            $"Cue {Defender} True 6 {ReadyCue.Damage("Guard Reversal", 6)}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityStatusSkillType} {(int)SkillType.Vibroblade} 4 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds} 4 4 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDamageBonus} 6 4 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");
    }

    [Test]
    public void GuardReversalWithOnlyADamageBonusKeepsAOneSecondWindow()
    {
        Stat(Defender, StatType.GuardedHitNextSkillAbilityStatusSkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 6);

        _guardedEffects.TrackGuardedHit(Defender);
        _log.Skip(1).Should().Equal(
            $"Cue {Defender} False 6 {ReadyCue.Damage("Guard Reversal", 6)}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityStatusSkillType} {(int)SkillType.Vibroblade} 1 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDamageBonus} 6 1 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");

        _log.Clear();
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 0);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds, 3);
        _guardedEffects.TrackGuardedHit(Defender);
        _log.Skip(1).Should().Equal(
            $"Cue {Defender} False 0 {ReadyCue.Damage("Guard Reversal", 0)}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityStatusSkillType} {(int)SkillType.Vibroblade} 3 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds} 3 3 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");
    }

    [Test]
    public void GuardReversalNeedsASkillAndADurationOrBonus()
    {
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 6);
        _guardedEffects.TrackGuardedHit(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 0);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityStatusSkillType, (int)SkillType.Vibroblade);
        _guardedEffects.TrackGuardedHit(Defender);
        Lines("Cue").Should().BeEmpty();
        Lines("Replace").Should().BeEmpty();
    }

    [Test]
    public void TheExposedBonusIsConsumedOnlyByItsSkill()
    {
        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Vibroblade;
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDamageBonus] = 6;

        _guardedEffects.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(Defender, SkillType.Spear).Should().Be(0);
        _guardedEffects.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(Defender, SkillType.Vibroblade).Should().Be(6);
        _log.Should().Equal($"Consume {Defender} {StatType.GuardedHitNextSkillAbilityExposedDamageBonus} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");
    }

    [Test]
    public void AGuardedHitReadiesTheNextAttackWithTheLongerWindowAndTellsAPlayer()
    {
        Stat(Attacker, StatType.GuardedHitNextAttackDMGBonus, 10);
        Stat(Attacker, StatType.GuardedHitNextAttackCriticalRatePercentAdjustment, 10);
        Stat(Attacker, StatType.GuardedHitNextAttackWindowSeconds, 30);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackDMGBonus, 8);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackEnmityBonus, 40);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackWindowSeconds, 20);

        _guardedEffects.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().Equal(
            $"Replace {Attacker} {StatType.NextAttackGuardedHitDMGBonus} 18 30 {StatType.NextAttackGuardedHitDMGBonus}",
            $"Replace {Attacker} {StatType.NextAttackGuardedHitCriticalRatePercentAdjustment} 10 30 {StatType.NextAttackGuardedHitDMGBonus}",
            $"Replace {Attacker} {StatType.NextAttackGuardedHitEnmityBonus} 40 30 {StatType.NextAttackGuardedHitDMGBonus}");
        FloatingText().Should().BeEmpty("an NPC sees no text");

        _players.Add(Attacker);
        _guardedEffects.ApplyGuardedHitNextAttackEffects(Attacker);
        FloatingText().Should().ContainSingle().Which.Should().Contain("Counter Ready: +18 DMG, +10% Crit, +40 Enmity");

        _engine.ClearCalls();
        Stat(Attacker, StatType.GuardedHitNextAttackCriticalRatePercentAdjustment, 0);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackEnmityBonus, 0);
        _guardedEffects.ApplyGuardedHitNextAttackEffects(Attacker);
        FloatingText().Should().ContainSingle().Which.Should().Contain("Counter Ready: +18 DMG").And.NotContain("Crit").And.NotContain("Enmity");
    }

    [Test]
    public void AGuardedHitWithoutAWindowOrABonusReadiesNothing()
    {
        Stat(Attacker, StatType.GuardedHitNextAttackDMGBonus, 10);
        _guardedEffects.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().BeEmpty("no window");

        Stat(Attacker, StatType.GuardedHitNextAttackDMGBonus, 0);
        Stat(Attacker, StatType.GuardedHitNextAttackWindowSeconds, 30);
        _guardedEffects.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().BeEmpty("no bonus");

        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackEnmityBonus, 5);
        _guardedEffects.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().HaveCount(3, "an Enmity bonus alone readies the attack");
    }
}
