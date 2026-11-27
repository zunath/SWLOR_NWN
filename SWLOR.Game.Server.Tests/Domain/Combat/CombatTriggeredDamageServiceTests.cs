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
/// The triggered damage part of <c>Combat</c>: the outgoing modifiers, the target adjustment, the resistance, the damage taken modifiers
/// and the effect of damage a trigger deals.
/// </summary>
[TestFixture]
public class CombatTriggeredDamageServiceTests : CombatHitEffectsTestBase
{
    [Test]
    public void TriggeredDamageRunsTheOutgoingTargetResistanceAndTakenModifiersThenItsEffects()
    {
        _typeDealtBonus = 10;
        Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, 50);
        _resisted = 5;
        _taken = damage => damage - 1;
        _queued = true;

        var (targeted, adjustment) = CombatRules.ApplyTriggeredDamageTargetAdjustment(30, 50, null);
        adjustment.Should().Be(50);

        _triggered.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Physical, SkillType.Rifle).Should().Be(targeted - 6);
        _log.Should().Equal(
            $"TypeDealt {Attacker} 20 {CombatDamageType.Physical}",
            $"Resist {Defender} {CombatDamageType.Physical} {targeted}",
            $"Taken {Defender} {targeted - 5} {Attacker} {CombatDamageType.Physical} {CombatDamageDeliveryType.Triggered} 50 True False",
            $"Queue {Attacker} {Defender} {targeted - 6} {CombatDamageType.Physical.GetNWScriptDamageType()}",
            $"Activity {Attacker}",
            $"Recent {Attacker} {Defender}",
            $"Notify {Attacker} {Defender} {targeted - 6} {CombatDamageType.Physical} {CombatDamageDeliveryType.Triggered}");
    }

    [Test]
    public void TriggeredForceDamageReadsTheForceTakenAdjustmentAndAnUnqueuedHitIsAssigned()
    {
        Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, -50);
        Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, 90);

        var (targeted, adjustment) = CombatRules.ApplyTriggeredDamageTargetAdjustment(20, -50, null);

        _triggered.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Force, typedLeadershipReductionAlreadyApplied: true)
            .Should().Be(targeted);
        Lines("Taken").Should().Equal($"Taken {Defender} {targeted} {Attacker} {CombatDamageType.Force} {CombatDamageDeliveryType.Triggered} {adjustment} True True");
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Attacker);
        RunDelayed();
        _engine.CallsTo("EffectDamage").Should().ContainSingle().Which.Arguments[0].Should().Be(targeted);
        _engine.CallsTo("ApplyEffectToObject").Should().ContainSingle().Which.Arguments[2].Should().Be(Defender);

        _log.Clear();
        _triggered.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Fire).Should().Be(20, "fire reads no typed target adjustment");
        _triggered.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Fire, targetStatusDamagePercentAdjustment: 0, outgoingModifiersAlreadyApplied: true)
            .Should().Be(20);
        Lines("TypeDealt").Should().ContainSingle("the second hit's outgoing modifiers were already applied");
        Lines("Taken").Last().Should().EndWith(" 0 True False");
    }

    [Test]
    public void TriggeredDamageThatComesToNothingStopsBeforeItsEffects()
    {
        _triggered.ApplyTriggeredDamage(Attacker, Defender, 0, CombatDamageType.Physical).Should().Be(0);
        _log.Should().BeEmpty();

        _resisted = 20;
        _triggered.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Physical).Should().Be(0);
        Lines("Taken").Should().BeEmpty("resisted entirely");

        _resisted = 0;
        _taken = _ => 0;
        _triggered.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Physical).Should().Be(0);
        Lines("Taken").Should().ContainSingle();
        Lines("Queue").Should().BeEmpty();
        Lines("Notify").Should().BeEmpty();
    }

    [Test]
    public void TheDamageTakenBackEdgeCallsTheUnmovedPartThroughCombat()
    {
        _triggered.BaseApplyDamageTakenModifiers(Defender, 0).Should().Be(0, "no damage is returned unchanged by the damage taken part");
        _triggered.BaseApplyDamageTakenModifiers(Defender, -4).Should().Be(-4);
    }

    [Test]
    public void TheDamageTakenBackEdgeKeepsTheDefaultsOfTheMemberItCalls()
    {
        var wrapper = typeof(CombatTriggeredDamageService).GetMethod("ApplyDamageTakenModifiers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = typeof(ICombatDamageTakenService).GetMethod("ApplyDamageTakenModifiers")!;
        wrapper.IsVirtual.Should().BeTrue();
        wrapper.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue))
            .Should().Equal(target.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue)));
    }

    [Test]
    public void ATriggeredHitRunsExactlyTheTrackingTheDealtEffectsRanForItBeforeTheCut()
    {
        // Before the cut, ApplyTriggeredDamage called ApplyDamageDealtEffects with the triggered delivery, which ran the tracking block
        // and returned. Now it calls the tracking member; the stay-part's own call for the same delivery runs the same lines.
        _directTrait = true;
        _service.ApplyDamageDealtEffects(Attacker, Defender, 20, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered);
        var original = _log.ToArray();
        original.Should().Equal($"Activity {Attacker}", $"Recent {Attacker} {Defender}");

        _log.Clear();
        _triggered.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Physical, SkillType.Rifle);
        var tracking = _log.FindLastIndex(line => line.StartsWith("Activity", StringComparison.Ordinal));
        _log.Skip(tracking).Take(original.Length).Should().Equal(original, "the triggered path runs the tracking block and nothing of the direct effects");
        _log.Skip(tracking + original.Length).Should().ContainSingle().Which.Should().StartWith("Notify");
        _log.Should().NotContain(line => line.StartsWith("Beast", StringComparison.Ordinal) || line.StartsWith("DirectHostile", StringComparison.Ordinal),
            "a triggered hit records no beast hit and no trait");
        Lines("Notify").Should().ContainSingle();
        _log.FindLastIndex(line => line.StartsWith("Notify", StringComparison.Ordinal))
            .Should().BeGreaterThan(_log.FindLastIndex(line => line.StartsWith("Activity", StringComparison.Ordinal)),
                "the status notice follows the tracking, as it did");
    }
}
