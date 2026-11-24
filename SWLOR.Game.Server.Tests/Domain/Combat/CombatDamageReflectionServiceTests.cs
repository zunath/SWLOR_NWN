using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The damage given back part of <c>Combat</c>: damage reflection, Deflecting Return and the incoming physical-to-Force conversion.
/// </summary>
[TestFixture]
public class CombatDamageReflectionServiceTests : CombatDamageTakenTestBase
{
    [TestCase(CombatDamageType.Physical, StatType.PhysicalDamageReflectionPercentAdjustment)]
    [TestCase(CombatDamageType.Force, StatType.ForceDamageReflectionPercentAdjustment)]
    [TestCase(CombatDamageType.Fire, StatType.ElementalDamageReflectionPercentAdjustment)]
    public void DamageIsReflectedByTheChannelOfItsType(CombatDamageType damageType, StatType channel)
    {
        Stat(Defender, channel, 25);

        _reflection.ApplyDamageReflectionEffects(Attacker, Defender, 40, damageType);
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Attacker} 10 {damageType} Invalid False null False");
        Lines("Attribute").Should().Equal($"Attribute {Defender} {channel}");
    }

    [Test]
    public void AForceHitWardsTheDefenderBeforeReflecting()
    {
        Stat(Defender, StatType.ForceDamageTakenForceDefense, 5);
        Stat(Defender, StatType.ForceDamageTakenForceDefenseDurationSeconds, 12);
        Stat(Defender, StatType.ForceDamageReflectionPercentAdjustment, 25);

        _reflection.ApplyDamageReflectionEffects(Attacker, Defender, 40, CombatDamageType.Force);
        _log.Should().Equal(
            $"Apply {Defender}>{Defender} ForceWarding 12 Force",
            $"Attribute {Defender} {StatType.ForceDamageReflectionPercentAdjustment}",
            $"Triggered {Defender} {Attacker} 10 Force Invalid False null False");

        _log.Clear();
        Stat(Defender, StatType.ForceDamageTakenForceDefenseDurationSeconds, 0);
        _reflection.ApplyDamageReflectionEffects(Attacker, Defender, 40, CombatDamageType.Force);
        Lines("Apply").Should().BeEmpty("a zero duration grants no ward");
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Attacker} 10 Force Invalid False null False");
    }

    [Test]
    public void ReflectionNeedsTwoDifferentValidCreaturesAndAPositiveHit()
    {
        Stat(Defender, StatType.PhysicalDamageReflectionPercentAdjustment, 25);
        _invalid.Add(Gone);
        _nonCreatures.Add(Other);

        _reflection.ApplyDamageReflectionEffects(Attacker, Defender, 0, CombatDamageType.Physical);
        _reflection.ApplyDamageReflectionEffects(Defender, Defender, 40, CombatDamageType.Physical);
        _reflection.ApplyDamageReflectionEffects(Gone, Defender, 40, CombatDamageType.Physical);
        _reflection.ApplyDamageReflectionEffects(Attacker, Gone, 40, CombatDamageType.Physical);
        _reflection.ApplyDamageReflectionEffects(Other, Defender, 40, CombatDamageType.Physical);
        _reflection.ApplyDamageReflectionEffects(Attacker, Other, 40, CombatDamageType.Physical);
        _reflection.ApplyDamageReflectionEffects(Attacker, Defender, 40, CombatDamageType.Fire);
        _log.Should().BeEmpty();
    }

    [Test]
    public void DeflectingReturnReflectsACappedShareOfWeaponDamageAsForceAndTellsNearbyPlayers()
    {
        Stat(Defender, StatType.RangedDeflectionReflectionPercent, 50);
        Stat(Defender, StatType.RangedDeflectionReflectionCapPercent, 100);
        _equippedSkill = SkillType.Vibroblade;
        _weaponDamage[Attacker] = 40;
        // Half of the attacker's 40 is 20, over the cap of the defender's own 10, so 10 is reflected.
        _weaponDamage[Defender] = 10;
        const int reflected = 10;
        _triggered = damage => damage - 1;

        _reflection.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(9);
        _log.Should().Equal(
            $"WeaponDamage {Attacker} Rifle False True null False",
            $"WeaponDamage {Defender} Vibroblade False False null False",
            $"Trigger {Defender} {StatType.RangedDeflectionReflectionPercent} 6",
            $"Attribute {Defender} {StatType.RangedDeflectionReflectionPercent}",
            $"Triggered {Defender} {Attacker} {reflected} Force Invalid False null False",
            $"Nearby {Defender} 60");
        _message!(Other).Should().Be(_reflection.BuildDeflectingReturnCombatLogMessage(Other, Defender, Attacker, 9));
    }

    [Test]
    public void TheDeflectingReturnLineNamesBothSidesAsTheObserverSeesThem()
    {
        _reflection.BuildDeflectingReturnCombatLogMessage(Other, Defender, Attacker, 9).Should().Be(
            ColorToken.Combat($"<{Other}:{Defender}>'s Deflecting Return reflects 9 Force damage to <{Other}:{Attacker}>."));
    }

    [Test]
    public void DeflectingReturnNeedsBothCreaturesARateDamageAReadyTriggerAndALandedReflection()
    {
        _invalid.Add(Gone);
        _reflection.ApplyRangedDeflectionReflection(Gone, Attacker, SkillType.Rifle).Should().Be(0);
        _reflection.ApplyRangedDeflectionReflection(Defender, Gone, SkillType.Rifle).Should().Be(0);
        _reflection.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        _log.Should().BeEmpty();

        Stat(Defender, StatType.RangedDeflectionReflectionPercent, 50);
        Stat(Defender, StatType.RangedDeflectionReflectionCapPercent, 100);
        _reflection.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        Lines("Trigger").Should().BeEmpty("no weapon damage, nothing to reflect");

        _weaponDamage[Attacker] = 40;
        _weaponDamage[Defender] = 30;
        _triggerReady = false;
        _reflection.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        Lines("Triggered").Should().BeEmpty();

        _triggerReady = true;
        _triggered = _ => 0;
        _reflection.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        Lines("Nearby").Should().BeEmpty();
    }

    [Test]
    public void TheConvertedShareLeavesThePhysicalHitAndArrivesAsAForceHitOnTheNextFrame()
    {
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 30);
        var physical = 100;
        var portion = CombatRules.GetIncomingPhysicalToForceConversionPortion(100, 30);

        _reflection.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref physical, -15).Should().Be(portion);
        physical.Should().Be(100 - portion);
        _log.Should().Equal($"Capture {Attacker}");
        var assigned = _engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Module);

        ((Action)assigned.Arguments[1]!)();
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(0f);
        ((Action)delayed.Arguments[1]!)();
        _log.Should().Equal($"Capture {Attacker}", "AttributeCaptured", $"Triggered {Attacker} {Defender} {portion} Force Invalid False -15 True");
    }

    [Test]
    public void AProjectileCarriesTheConvertedHitUntilItArrivesAndAGoneCreatureTakesNothing()
    {
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 30);
        var physical = 100;
        Action? carried = null;

        _reflection.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref physical, 0, action => carried = action)
            .Should().BeGreaterThan(0);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();

        _invalid.Add(Defender);
        carried!();
        _log.Should().Equal($"Capture {Attacker}", "AttributeCaptured");
        _invalid.Clear();
        _invalid.Add(Attacker);
        carried();
        Lines("Triggered").Should().BeEmpty();
    }

    [Test]
    public void NothingConvertsFromANonPhysicalOrEmptyHitOrWithoutAConversion()
    {
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 30);
        var damage = 100;
        _reflection.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Fire, ref damage).Should().Be(0);
        var none = 0;
        _reflection.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref none).Should().Be(0);
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 0);
        _reflection.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref damage).Should().Be(0);
        damage.Should().Be(100);
        _log.Should().BeEmpty();
    }
}
