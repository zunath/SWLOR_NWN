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
/// The guarded hit part of <c>Combat</c>: the Guard roll, the reduction and the riders a landed Guard runs in order.
/// </summary>
[TestFixture]
public class CombatGuardedHitServiceTests : CombatDamageTakenTestBase
{
    [Test]
    public void AGuardedHitLosesItsGuardShareAndRunsEveryRiderInOrder()
    {
        _guardChance = 100;
        _hostile.Add((Attacker, Defender));
        Stat(Defender, StatType.GuardDamageReductionPercentAdjustment, 10);
        Stat(Defender, StatType.GuardStaminaRestore, 4);
        Stat(Defender, StatType.GuardStaminaRestoreCooldownSeconds, 12);
        Stat(Defender, StatType.GuardEnmityPercentAdjustment, 50);

        var prevented = Math.Min(100, GameMath.PercentOf(100, 30));
        _guardedHit.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100 - prevented);
        _log.Should().Equal(
            $"NextSkill {Defender} Invalid 0 0 0 0 None",
            $"OnGuarded {Defender} {Attacker} {prevented}",
            $"Trigger {Defender} {StatType.GuardStaminaRestore} 12",
            $"Stamina {Defender} {StatType.GuardStaminaRestore} 4",
            $"Enmity {Defender} {Attacker} 150",
            $"Feedback {Defender} {Attacker} {prevented}");
        GuardedHits()[Defender].Should().Be(_now);
    }

    [Test]
    public void AGuardedHitIsOneRollAgainstTheGuardChance()
    {
        _hostile.Add((Attacker, Defender));
        var rolls = Rolls(11, 2);
        _guardChance = rolls[0] - 1;

        _guardedHit.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _log.Should().BeEmpty();
        Random.D100(1).Should().Be(rolls[1], "the guard drew once");
    }

    [Test]
    public void GuardOnlyAppliesToALandedPositivePhysicalHitFromAHostileSource()
    {
        _guardChance = 100;
        _invalid.Add(Gone);

        _guardedHit.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, false).Should().Be(100);
        _guardedHit.ApplyGuardedHitModifiers(Gone, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _guardedHit.ApplyGuardedHitModifiers(Defender, Gone, 100, CombatDamageType.Physical, true).Should().Be(100);
        _guardedHit.ApplyGuardedHitModifiers(Defender, Defender, 100, CombatDamageType.Physical, true).Should().Be(100);
        _guardedHit.ApplyGuardedHitModifiers(Defender, Attacker, 0, CombatDamageType.Physical, true).Should().Be(0);
        _guardedHit.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Fire, true).Should().Be(100);
        _guardedHit.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _guardChance = 0;
        _hostile.Add((Attacker, Defender));
        _guardedHit.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _log.Should().BeEmpty();
    }

    [Test]
    public void GuardRecoveryNeedsItsStaminaAndAReadyTrigger()
    {
        PrivateOf(_guardedHit, "ApplyGuardedHitRecovery", Defender);
        Stat(Defender, StatType.GuardStaminaRestore, 4);
        _triggerReady = false;
        PrivateOf(_guardedHit, "ApplyGuardedHitRecovery", Defender);
        _log.Should().Equal($"Trigger {Defender} {StatType.GuardStaminaRestore} 0");
    }

    [Test]
    public void GuardEnmityIsAtLeastOneAndScaledByItsAdjustment()
    {
        PrivateOf(_guardedHit, "ApplyGuardedHitEnmity", Attacker, Defender, 0);
        PrivateOf(_guardedHit, "ApplyGuardedHitEnmity", Attacker, Defender, 9);
        Stat(Defender, StatType.GuardEnmityPercentAdjustment, -99);
        PrivateOf(_guardedHit, "ApplyGuardedHitEnmity", Attacker, Defender, 9);
        _log.Should().Equal($"Enmity {Defender} {Attacker} 1", $"Enmity {Defender} {Attacker} 9", $"Enmity {Defender} {Attacker} 1");
    }
}
