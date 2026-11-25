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
/// The defeated enemy effects part of <c>Combat</c>: what a kill sets off for the killer, the recent damagers and the nearby enemies.
/// </summary>
[TestFixture]
public class CombatDefeatedEnemyEffectsServiceTests : CombatHitEffectsTestBase
{
    [Test]
    public void DefeatingAnEnemyRestoresHealsAndBuffsInOrder()
    {
        Stat(Attacker, StatType.DefeatedEnemyStaminaRestore, 2);
        Stat(Attacker, StatType.DefeatedEnemyFPRestore, 3);
        Stat(Attacker, StatType.DefeatedEnemyHPPercentRestore, 5);
        Stat(Attacker, StatType.DefeatedEnemyAttackPercentAdjustment, 10);
        Stat(Attacker, StatType.DefeatedEnemyAttackDurationSeconds, 6);
        Stat(Attacker, StatType.DefeatedEnemyAttackDelayReductionPercent, 12);
        Stat(Attacker, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds, 7);
        Stat(Attacker, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment, 8);
        Stat(Attacker, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds, 9);
        _party.AddRange(new[] { Attacker, Ally });

        _defeated.ApplyDefeatedEnemyEffects(Attacker);
        _log.Should().Equal(
            $"Attribute {Attacker} {CombatRecordSourceKind.OnKill}",
            $"RestoreStamina {Attacker} 2",
            $"RestoreFP {Attacker} 3",
            $"HealMax {Attacker} 5",
            $"Replace {Attacker} {StatType.AttackPercentAdjustment} 10 6 {StatType.DefeatedEnemyAttackPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 12 7 {StatType.DefeatedEnemyAttackDelayReductionPercent}",
            $"Party {Attacker} 5",
            $"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 8 9 {StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment}");

        _log.Clear();
        _dead.Add(Attacker);
        _defeated.ApplyDefeatedEnemyEffects(Attacker);
        _invalid.Add(Gone);
        _defeated.ApplyDefeatedEnemyEffects(Gone);
        _log.Should().BeEmpty();
        _dead.Clear();
        _stats.Clear();
        _defeated.ApplyDefeatedEnemyEffects(Attacker);
        _log.Should().Equal($"Attribute {Attacker} {CombatRecordSourceKind.OnKill}");
    }

    [Test]
    public void SoulAscensionFollowsAKillInsideTheHitPointSpendWindow()
    {
        Stat(Attacker, StatType.HeavyVibrobladeOffenseSoulAscension, 1);
        _defeated.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().BeEmpty("no hit points were spent");
        _temporary[StatType.HeavyVibrobladeOffenseSoulAscension] = 1;
        _defeated.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Attacker} {nameof(SoulAscensionStatusEffect)} 30");
        _log.Clear();
        Stat(Attacker, StatType.HeavyVibrobladeOffenseSoulAscension, 0);
        _defeated.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ADefeatedEnemysPoisonSpreadsFromItsSourceToTheNearestEnemy()
    {
        var effects = new CreatureStatusEffect { StatGroup = new StatGroup() };
        var poison = new PoisonStatusEffect();
        typeof(StatusEffectBase).GetProperty(nameof(StatusEffectBase.Source))!.SetValue(poison, Ally);
        effects.Add(new BleedStatusEffect());
        effects.Add(poison);
        _creatureEffects[Defender] = effects;
        Stat(Ally, StatType.PoisonedDefeatedEnemySpreadRadiusMeters, 6);
        Stat(Ally, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 12);
        _nearest = Other;

        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _log.Should().ContainInOrder(
            $"Nearest {Ally} {Defender} 6 {Defender}",
            $"Apply {Ally}>{Other} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Physical}");

        _log.Clear();
        _invalid.Add(Ally);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadRadiusMeters, 6);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 12);
        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Physical}");

        _log.Clear();
        _nearest = NWScript.OBJECT_INVALID;
        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 0);
        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _creatureEffects.Clear();
        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ADefeatedBleedingEnemysBleedSpreadsToNearbyEnemies()
    {
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 8);
        _nearby.Add(Other);

        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(BleedStatusEffect)} 8 {CombatDamageType.Physical}");

        _log.Clear();
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 0);
        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _categories.Clear();
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 8);
        _defeated.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _invalid.Add(Gone);
        Private("ApplyDefeatedBleedingEnemySpread", Gone, Defender);
        Private("ApplyDefeatedBleedingEnemySpread", Attacker, Gone);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void EveryRecentDamagerOfTheDefeatedGainsCruelMomentumAndStaleRecordsArePruned()
    {
        Stat(Attacker, StatType.CruelMomentum, 1);
        Stat(Other, StatType.CruelMomentum, 1);
        _recent[(Attacker, Defender)] = _now.AddSeconds(-6);
        _recent[(Other, Defender)] = _now.AddSeconds(-7);
        _recent[(Attacker, Ally)] = _now.AddSeconds(-30);

        _defeated.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CruelMomentum} 10",
            $"FP {Attacker} {StatType.CruelMomentum} 2",
            $"Apply {Attacker}>{Attacker} {nameof(CruelMomentumStatusEffect)} 30");
        _recent.Keys.Should().BeEquivalentTo(new[] { (Attacker, Defender), (Attacker, Ally) }, "the stale record of the defeated is pruned");

        _log.Clear();
        _triggerReady = false;
        _defeated.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CruelMomentum} 10");
        _log.Clear();
        _dead.Add(Attacker);
        _defeated.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _dead.Clear();
        Stat(Attacker, StatType.CruelMomentum, 0);
        _defeated.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _invalid.Add(Gone);
        _defeated.ApplyRecentDamagerDefeatedEnemyEffects(Gone);
        _invalid.Add(Attacker);
        _defeated.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ToxicPoisonSpreadsFromATargetToNearbyEnemies()
    {
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters, 5);
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds, 9);
        _nearby.Add(Other);

        _defeated.SpreadPoisonFromTarget(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(PoisonStatusEffect)} 9 {CombatDamageType.Poison}");
        _log.Clear();
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds, 0);
        _defeated.SpreadPoisonFromTarget(Attacker, Defender);
        _log.Should().BeEmpty();
    }
}
