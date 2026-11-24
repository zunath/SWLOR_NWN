using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability hit roll of <c>Combat</c>: hit chance and its roll and the test overrides of the hit rolls.
/// </summary>
[TestFixture]
public class CombatAbilityHitServiceTests : CombatAbilityHitTestBase
{
    // ------------------------------------------------------------------ hit roll

    [Test]
    public void AnInvalidRollIsAHitAtFullRateWithoutDrawing()
    {
        _invalid.Add(Gone);
        var rolls = Rolls(5, 1);

        _service.TryResolveAbilityHit(Gone, Defender, SkillType.Rifle, PerkType.Invalid, out var hitRate).Should().BeTrue();
        hitRate.Should().Be(100);
        _service.TryResolveAbilityHit(Attacker, Gone, SkillType.Rifle, PerkType.Invalid, out hitRate).Should().BeTrue();
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Invalid, PerkType.Invalid, out hitRate).Should().BeTrue();
        hitRate.Should().Be(100);
        Random.D100(1).Should().Be(rolls[0]);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheHitRateAddsEveryAdjustmentAndTheRollDrawsOnce()
    {
        _accuracy = 200;
        _traitAccuracy = 7;
        _statusAccuracy = 3;
        _evasion = 150;
        _evasionIgnore = 10;
        _skillSelected[StatType.AbilityHitChancePercentAdjustment] = 2;
        _skillSelected[StatType.IncomingAbilityHitChancePercentAdjustment] = -4;
        _longRangeHit = 3;
        _targeted = 5;
        _physicalForce = 6;
        _sideHit = 1;
        _idleHit = 8;
        _suppressionAccuracy = 9;
        _minimumHitRate = 20;
        Stat(Attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment, 11);
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        var expected = CombatRules.CalculateHitRate(200 + 7 + 3 + 4, 150 - 10, 1 + 2 + 3 + 5 + 6 - 4 + 1 + 8 + 9 + 11, 20);

        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] <= expected);
        var rolls = Rolls(seed, 2);
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.DoubleShot, out var hitRate, 1, 30, AbilityType.Might, 4)
            .Should().BeTrue();
        hitRate.Should().Be(expected);
        Random.D100(1).Should().Be(rolls[1], "the hit roll drew exactly one number");
        _log.Should().Contain($"Accuracy {Attacker} {Weapon} {AbilityType.Might} {SkillType.Rifle} 30 False");
        _log.Should().Contain($"Evasion {Defender} {SkillType.Invalid} {SkillType.Rifle}");
        _log.Should().Contain($"Targeted {Attacker} {PerkType.DoubleShot} {StatType.TargetedAbilityHitChancePercentAdjustment}");
        _log.Should().Contain($"Selected {Defender} {SkillType.Rifle} {StatType.IncomingAbilityHitChancePercentAdjustmentSkillType} {StatType.IncomingAbilityHitChancePercentAdjustment} True");
        _log.Should().NotContain(line => line.StartsWith("Affinity") || line.StartsWith("Miss"));
    }

    [Test]
    public void AForceAbilityUsesWillpowerAccuracyAndItsAffinity()
    {
        _forceAffinity = 12;
        _evasion = 1000;
        var expected = CombatRules.CalculateHitRate(0, 1000, 12, 0);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > expected);
        Rolls(seed, 1);

        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.ForceLightning, out var hitRate).Should().BeFalse();
        hitRate.Should().Be(expected);
        _log.Should().Contain($"Accuracy {Attacker} {Weapon} {AbilityType.Willpower} {SkillType.Force} -1 True");
        _log.Should().Contain($"Affinity {Attacker} {PerkType.ForceLightning}");
        _log.Should().Contain($"Miss {Defender}", "a missed hostile ability counts as an accuracy or evasion miss");
    }

    [Test]
    public void AMissOnAFriendlyTargetIsNotRecordedAndTheOverridesDecideWithoutDrawing()
    {
        _hostile = false;
        _evasion = 1000;
        var expected = CombatRules.CalculateHitRate(0, 1000, 0, 0);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > expected);
        Rolls(seed, 1);
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out _).Should().BeFalse();
        _log.Should().NotContain(line => line.StartsWith("Miss"));

        _service.SetAbilityHitResolutionOverride(true);
        var rolls = Rolls(9, 1);
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out var hitRate).Should().BeTrue();
        hitRate.Should().Be(expected, "the rate is still computed");
        Random.D100(1).Should().Be(rolls[0], "a forced outcome draws no roll");

        _service.SetAbilityHitResolutionOverride(false);
        _evasion = 0;
        _hostile = true;
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out _).Should().BeFalse();
        _log.Should().Contain($"Miss {Defender}");
        _service.SetAbilityHitResolutionOverride(null);
    }

    [Test]
    public void TheAutoAttackOverrideIsKeptForTheNativeRoll()
    {
        _service.GetAutoAttackHitResolutionOverride().Should().BeNull();
        _service.SetAutoAttackHitResolutionOverride(false);
        _service.GetAutoAttackHitResolutionOverride().Should().BeFalse();
        _service.SetAutoAttackHitResolutionOverride(null);
        _service.GetAutoAttackHitResolutionOverride().Should().BeNull();
    }

    [Test]
    public void AnExplicitImpactProfileGivesTheAccuracyOfItsCapturedWeapon()
    {
        var weapon = new WeaponShotProfile(Weapon, SkillType.Pistol, 10, CombatDamageType.Physical, 90, 90, AbilityType.Perception, "1");
        using var profile = new WeaponImpactProfile(Attacker, weapon, new WeaponImpactActivation(), accuracyAdjustment: 6);
        _traitAccuracy = 4;
        _statusAccuracy = 2;
        _accuracy = 1;

        _service.SetAbilityHitResolutionOverride(true);
        try
        {
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Pistol, PerkType.Invalid, out var hitRate).Should().BeTrue();
            hitRate.Should().Be(CombatRules.CalculateHitRate(90 + 6 + 4 + 2, 0, 0, 0));
        }
        finally
        {
            _service.SetAbilityHitResolutionOverride(null);
        }

        _log.Should().NotContain(line => line.StartsWith("Accuracy"), "the captured accuracy replaces the equipped weapon's");
    }

    [Test]
    public void TheContainerBuildsThePartWithItsInjectedNeighbours()
    {
        _facades.Get<ICombatAbilityHitService>().Should().BeOfType<CombatAbilityHitService>();
    }
}
