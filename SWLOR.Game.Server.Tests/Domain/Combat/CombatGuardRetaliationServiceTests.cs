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
/// The guard retaliation part of <c>Combat</c>: the retaliation, its pulse and the damage roll they use.
/// </summary>
[TestFixture]
public class CombatGuardRetaliationServiceTests : CombatDamageTakenTestBase
{
    [Test]
    public void GuardRetaliationResolvesItsDamageRangeAndDealsItAsTriggeredPhysicalDamage()
    {
        _equippedSkill = SkillType.Vibroblade;
        _weapon = Weapon;
        _damageAbility = AbilityType.Perception;
        _calculated = 17;
        _defenseBonus = 5;
        _attackBonus = 3;
        Stat(Defender, StatType.GuardRetaliationDMG, 8);

        _retaliation.ApplyGuardedHitRetaliation(Attacker, Defender, 50);
        var perception = 10 + (int)AbilityType.Perception;
        var vitality = 10 + (int)CombatDamageType.Physical.GetDefenseAbilityType();
        _log.Should().Equal(
            $"Weapon {Defender} Vibroblade",
            $"DamageAbility {Defender} {BaseItem.Longsword}",
            $"Attack {Defender} {AbilityType.Perception} Vibroblade",
            $"TargetAttack {Defender} {Attacker} 100 Vibroblade",
            $"Defense {Attacker} Physical {CombatDamageType.Physical.GetDefenseAbilityType()}",
            $"SourceDefense {Defender} {Attacker} 60",
            $"Calculate 103 8 {perception} 65 {vitality} 0 0",
            $"Attribute {Defender} {StatType.GuardRetaliationDMG}",
            $"Triggered {Defender} {Attacker} 17 Physical Vibroblade False null False");
    }

    [Test]
    public void AGuardRetaliationAgainstAForceConvertingTargetBlendsInItsForceDefense()
    {
        _calculated = 4;
        Stat(Defender, StatType.GuardRetaliationDMG, 8);
        Stat(Attacker, StatType.IncomingPhysicalToForceConversionPercent, 50);

        _retaliation.ApplyGuardedHitRetaliation(Attacker, Defender, 50);
        Lines("Defense").Should().Equal(
            $"Defense {Attacker} Physical {CombatDamageType.Physical.GetDefenseAbilityType()}",
            $"Defense {Attacker} Force {CombatDamageType.Force.GetDefenseAbilityType()}");
        Lines("SourceDefense").Should().Equal($"SourceDefense {Defender} {Attacker} 60", $"SourceDefense {Defender} {Attacker} 40");
        // Without a weapon the retaliation uses Might.
        Lines("Attack").Should().Equal($"Attack {Defender} {AbilityType.Might} Invalid");
        Lines("Calculate").Should().ContainSingle().Which.Should().StartWith("Calculate 100 8 ");
        Lines("Calculate").Single().Split(' ')[4].Should().Be(((60 + 40) / 2).ToString());
    }

    [Test]
    public void AWeaponWithoutADamageAbilityRetaliatesWithMight()
    {
        _weapon = Weapon;
        Stat(Defender, StatType.GuardRetaliationDMG, 8);

        _retaliation.ApplyGuardedHitRetaliation(Attacker, Defender, 50);
        Lines("Attack").Should().Equal($"Attack {Defender} {AbilityType.Might} Invalid");
    }

    [Test]
    public void ARetaliationAgainstAMissingTargetResolvesNothing()
    {
        _invalid.Add(Gone);
        Stat(Defender, StatType.GuardRetaliationDMG, 8);

        _retaliation.ApplyGuardedHitRetaliation(Gone, Defender, 50);
        Lines("Calculate").Should().BeEmpty();
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Gone} 0 Physical Invalid False null False");
        PrivateOf(_retaliation, "ResolveGuardRetaliationDamage", Gone, Attacker, 8, SkillType.Invalid).Should().Be(0);
        PrivateOf(_retaliation, "ResolveGuardRetaliationDamage", Defender, Attacker, 0, SkillType.Invalid).Should().Be(0);
    }

    [Test]
    public void AGuardPulseWithoutARadiusHitsOnlyTheAttacker()
    {
        _calculated = 6;
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);

        _retaliation.ApplyGuardedHitRetaliation(Attacker, Defender, 50);
        Lines("Attribute").Should().Equal($"Attribute {Defender} {StatType.GuardedHitPulseDMG}");
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Attacker} 6 Physical Invalid False null False");
    }

    [Test]
    public void AGuardPulseHitsTheAttackerAndEveryHostileCreatureAroundAndTellsAPlayer()
    {
        _players.Add(Defender);
        _calculated = 6;
        Stat(Defender, StatType.GuardRetaliationDMG, 0);
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);
        Stat(Defender, StatType.GuardedHitPulseRadiusMeters, 4);
        Stat(Defender, StatType.GuardedHitPulseEnmityPercentOfIncomingDamage, 10);
        _hitPoints[Attacker] = (50, 50);
        _hitPoints[Other] = (50, 50);
        _hitPoints[Ally] = (50, 50);
        _hostile.Add((Attacker, Defender));
        _hostile.Add((Other, Defender));
        _inShape.AddRange(new[] { Attacker, Gone, Other, Ally });
        _vanishAfterSearch.Add(Gone);

        _retaliation.ApplyGuardedHitRetaliation(Attacker, Defender, 50);
        _log.Where(line => line.StartsWith("Triggered", StringComparison.Ordinal) || line.StartsWith("Enmity", StringComparison.Ordinal)).Should().Equal(
            $"Triggered {Defender} {Attacker} 6 Physical Invalid False null False",
            $"Triggered {Defender} {Other} 6 Physical Invalid False null False",
            $"Enmity {Defender} {Other} 5");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Retaliation Pulse")}");
    }

    [Test]
    public void AGuardPulseThatFindsNoLivingHostileTargetDoesNothing()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);
        Stat(Defender, StatType.GuardedHitPulseRadiusMeters, 4);
        _hitPoints[Other] = (0, 50);
        _hitPoints[Ally] = (50, 50);
        _dead.Add(Attacker);
        _hostile.Add((Other, Defender));
        _hostile.Add((Attacker, Defender));
        _inShape.AddRange(new[] { Other, Ally, Attacker });

        _retaliation.ApplyGuardedHitRetaliation(Attacker, Defender, 50);
        Lines("Triggered").Should().BeEmpty();
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void AGuardPulseAddsNoEnmityWithoutItsPercentAndNoTextForANonPlayer()
    {
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);
        Stat(Defender, StatType.GuardedHitPulseRadiusMeters, 4);
        _hitPoints[Other] = (50, 50);
        _hostile.Add((Other, Defender));
        _inShape.Add(Other);
        _invalid.Add(Gone);

        _retaliation.ApplyGuardedHitRetaliation(Gone, Defender, 50);
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Other} 0 Physical Invalid False null False");
        Lines("Enmity").Should().BeEmpty();
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void TheDamageRollRunsOnTheDamageRollPartWithTheSharedRandom()
    {
        Random.SetSeed(21);
        var expected = GameServices.CombatDamageRoll.CalculateDamage(100, 8, 14, 60, 12, 0);
        Random.SetSeed(21);

        _retaliation.BaseCalculateDamage(100, 8, 14, 60, 12, 0).Should().Be(expected);
    }

    [Test]
    public void TheDamageRollWrapperKeepsTheDefaultsOfTheDamageRollPart()
    {
        var wrapper = typeof(CombatGuardRetaliationService).GetMethod("CalculateDamage", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = typeof(ICombatDamageRollService).GetMethod("CalculateDamage")!;

        wrapper.IsVirtual.Should().BeTrue();
        wrapper.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue))
            .Should().Equal(target.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue)));
    }
}
