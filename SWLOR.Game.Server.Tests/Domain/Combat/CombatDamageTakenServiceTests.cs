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
/// The damage taken part of <c>Combat</c>: the modifiers of an incoming hit, fatal damage prevention, Guard and its riders,
/// and the physical-to-Force defense conversion.
/// </summary>
[TestFixture]
public class CombatDamageTakenServiceTests : CombatDamageTakenTestBase
{
    // ------------------------------------------------------------------ the damage taken modifiers

    [Test]
    public void ANonPositiveHitKeepsItsDamageAndTouchesNothing()
    {
        _service.ApplyDamageTakenModifiers(Defender, 0, Attacker).Should().Be(0);
        _service.ApplyDamageTakenModifiers(Defender, -4, Attacker).Should().Be(-4);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AnImmuneDefenderTakesNothingAndNothingElseRuns()
    {
        _immune.Add(Defender);

        _service.ApplyDamageTakenModifiers(Defender, 50, Attacker, CombatDamageType.Fire).Should().Be(0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ALandedPhysicalHitStacksThePvpIncreaseTheLeadershipStageTheGenericAndFlatAdjustmentsInThatOrder()
    {
        _pvpBonus = 10;
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -10);
        Stat(Defender, StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment, -5);
        Stat(Defender, StatType.LeadershipForceDamageTakenPercentAdjustment, -40);
        Stat(Defender, StatType.LeadershipOtherDamageTakenPercentAdjustment, -40);
        Stat(Defender, StatType.DamageTakenPercentAdjustment, -20);
        Stat(Defender, StatType.DamageTakenFlatAdjustment, 3);

        var expected = CombatRules.ApplyCombinedDamageTakenAdjustment(CombatRules.ApplyPercentDamageAdjustment(110, -15), -10, -20) + 3;
        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, -10)
            .Should().Be(expected);
        _log.Should().Equal($"Pvp {Defender} {Attacker} 100 True", $"RefreshHealing {Attacker} {Defender}");
    }

    [TestCase(CombatDamageType.Force, StatType.LeadershipForceDamageTakenPercentAdjustment, StatType.LeadershipRecoveryForceDamageTakenPercentAdjustment)]
    [TestCase(CombatDamageType.Fire, StatType.LeadershipOtherDamageTakenPercentAdjustment, StatType.Invalid)]
    public void AForceOrOtherHitUsesItsOwnLeadershipChannel(CombatDamageType damageType, StatType channel, StatType recovery)
    {
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -50);
        Stat(Defender, channel, -20);
        if (recovery != StatType.Invalid)
            Stat(Defender, recovery, -10);
        var total = recovery == StatType.Invalid ? -20 : -30;

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, damageType)
            .Should().Be(CombatRules.ApplyCombinedDamageTakenAdjustment(CombatRules.ApplyPercentDamageAdjustment(100, total), 0, 0));
        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, damageType).Should().Be(CombatRules.ApplyPercentDamageAdjustment(100, total));
    }

    [Test]
    public void TheTypedLeadershipStageReadsThePhysicalChannelAndKeepsDamageWithoutAnAdjustment()
    {
        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, CombatDamageType.Physical).Should().Be(100);
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -10);
        Stat(Defender, StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment, -10);

        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, CombatDamageType.Physical).Should().Be(80);
        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, CombatDamageType.Force).Should().Be(100);
    }

    [Test]
    public void ALeadershipReductionAlreadyAppliedIsNotAppliedTwice()
    {
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -50);
        Stat(Defender, StatType.DamageTakenPercentAdjustment, -20);

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, true, true)
            .Should().Be(CombatRules.ApplyCombinedDamageTakenAdjustment(100, 0, -20));
    }

    [Test]
    public void ReductionsNeverTakeALandedHitBelowOne()
    {
        Stat(Defender, StatType.DamageTakenFlatAdjustment, -50);

        _service.ApplyDamageTakenModifiers(Defender, 10, Attacker).Should().Be(1);
    }

    [Test]
    public void ASwingTheEngineMayDiscardStopsAfterTheMathAndSpendsNothing()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 50);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;

        _service.ApplyDamageTakenModifiers(Defender, 40, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, false)
            .Should().Be(40);
        _log.Should().Equal($"Pvp {Defender} {Attacker} 40 False");
    }

    [Test]
    public void TransferredDamageIsNotRaisedAgainButARecordedLandedHitKeepsThePvpFlag()
    {
        _pvpBonus = 99;

        _service.ApplyDamageTakenModifiers(Defender, 40, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred)
            .Should().Be(40);
        _service.ApplyDamageTakenModifiers(Defender, 40, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred, 0, false)
            .Should().Be(40);
        _log.Should().Equal($"PvpHit {Attacker} {Defender}", $"RefreshHealing {Attacker} {Defender}");
    }

    [Test]
    public void ARecordedHitTellsTheRecorderToExpectTheDeferredDamage()
    {
        _recording = true;

        _service.ApplyDamageTakenModifiers(Defender, 30, Attacker, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered).Should().Be(30);
        _log.Should().Equal($"Pvp {Defender} {Attacker} 30 True", $"RefreshHealing {Attacker} {Defender}", $"Expect {Attacker} {Defender} 30 Triggered");
    }

    [Test]
    public void ARedirectSendsItsShareToTheStatusSourceAsTransferredDamageUnderTheAttacker()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 40);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;
        _hostile.Add((Attacker, Ally));

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Fire).Should().Be(60);

        _log.Should().Equal(
            $"Pvp {Defender} {Attacker} 100 True",
            $"RemoveWithStat {Defender} {StatType.DamageTakenRedirectToStatusSourcePercent} False",
            $"PvpHit {Attacker} {Ally}",
            $"RefreshHealing {Attacker} {Ally}",
            $"Enmity {Ally} {Attacker} 40",
            $"RefreshHealing {Attacker} {Defender}");
        var assigned = _engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Attacker);
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Holy_Aid);
        EffectTargets().Should().Equal(Ally);

        RunCommands();
        _engine.CallsTo("EffectDamage").Should().ContainSingle().Which.Arguments[0].Should().Be(40);
        EffectTargets().Should().Equal(Ally, Ally);
    }

    [Test]
    public void ARedirectIsCappedAtTheWholeHitAndTheDefenderTakesNothingWhenItIsAllRedirected()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 250);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;
        _invalid.Add(Gone);

        _service.ApplyDamageTakenModifiers(Defender, 50, Gone).Should().Be(0);

        // The attacker is gone, so the redirected hit is dealt under the defender and nobody gains enmity.
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Defender);
        Lines("Enmity").Should().BeEmpty();
        Lines("RefreshHealing").Should().Equal($"RefreshHealing {Gone} {Ally}");
    }

    [Test]
    public void ARedirectTheSourceIsImmuneToStillShowsTheAidButDealsAndAngersNothing()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 50);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;
        _immune.Add(Ally);
        _hostile.Add((Attacker, Ally));

        _service.ApplyDamageTakenModifiers(Defender, 20, Attacker).Should().Be(10);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Holy_Aid);
        Lines("Enmity").Should().BeEmpty();
    }

    [TestCase("missing")]
    [TestCase("dead")]
    [TestCase("self")]
    public void ARedirectNeedsALivingStatusSourceOtherThanTheDefender(string source)
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 50);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = source switch
        {
            "missing" => NWScript.OBJECT_INVALID,
            "self" => Defender,
            _ => Ally
        };
        if (source == "dead")
            _dead.Add(Ally);

        _service.ApplyDamageTakenModifiers(Defender, 20, Attacker).Should().Be(20);
        Lines("RemoveWithStat").Should().BeEmpty();
    }

    [Test]
    public void AShareCarriesItsPartToTheBondedSourceAsForceDamage()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;
        Stat(Ally, StatType.LeadershipForceDamageTakenPercentAdjustment, -50);
        _hostile.Add((Attacker, Ally));

        var shared = GameMath.PercentOf(100, 30);
        var taken = CombatRules.ApplyCombinedDamageTakenAdjustment(CombatRules.ApplyPercentDamageAdjustment(shared, -50), 0, 0);
        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Fire).Should().Be(100 - shared);

        _log.Should().Equal(
            $"Pvp {Defender} {Attacker} 100 True",
            $"PvpHit {Attacker} {Ally}",
            $"RefreshHealing {Attacker} {Ally}",
            $"Enmity {Ally} {Attacker} {taken}",
            $"RefreshHealing {Attacker} {Defender}");
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Attacker);
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Holy_Aid);
        RunCommands();
        var damage = _engine.CallsTo("EffectDamage").Should().ContainSingle().Subject;
        damage.Arguments[0].Should().Be(taken);
        damage.Arguments[1].Should().Be(CombatDamageType.Force.GetNWScriptDamageType());
    }

    [Test]
    public void AShareTheBondedSourceTakesNothingFromShowsNoAidAndAngersNobody()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;
        _immune.Add(Ally);
        _invalid.Add(Gone);

        _service.ApplyDamageTakenModifiers(Defender, 100, Gone).Should().Be(100 - GameMath.PercentOf(100, 30));
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
        Visuals().Should().BeEmpty();
        Lines("Enmity").Should().BeEmpty();
    }

    [Test]
    public void AShareUnderAMissingAttackerIsDealtUnderTheDefender()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;
        _invalid.Add(Gone);

        _service.ApplyDamageTakenModifiers(Defender, 100, Gone).Should().Be(100 - GameMath.PercentOf(100, 30));
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Defender);
        Lines("Enmity").Should().BeEmpty();
    }

    [TestCase("missing")]
    [TestCase("dead")]
    [TestCase("self")]
    [TestCase("elsewhere")]
    public void AShareNeedsALivingBondedSourceInTheSameArea(string source)
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = source switch
        {
            "missing" => NWScript.OBJECT_INVALID,
            "self" => Defender,
            _ => Ally
        };
        if (source == "dead")
            _dead.Add(Ally);
        if (source == "elsewhere")
            _areas[Ally] = AreaTwo;

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker).Should().Be(100);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void TransferredDamageIsNotSharedAgain()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred)
            .Should().Be(100);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void TheRedirectAndShareStagesKeepADamageAlreadyGone()
    {
        Private("ApplyDamageTakenRedirectToStatusSource", Defender, Attacker, 0, CombatDamageType.Physical).Should().Be(0);
        Private("ApplyDamageTakenShareToStatusSource", Defender, Attacker, -2, CombatDamageType.Physical).Should().Be(-2);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AHitTheFatalDamageSaveCatchesDealsNothing()
    {
        _hitPoints[Defender] = (20, 100);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);

        _service.ApplyDamageTakenModifiers(Defender, 30, Attacker).Should().Be(0);
        Lines("RefreshHealing").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle();
    }

    [Test]
    public void ALandedHitThatWouldDropTheDefenderBelowTheThresholdGrantsTheLowHPShieldFirst()
    {
        _hitPoints[Defender] = (60, 100);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 20);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 6);
        Stat(Defender, StatType.LowHPTemporaryHPCooldownSeconds, 30);

        _service.ApplyDamageTakenModifiers(Defender, 70, Attacker).Should().Be(70);
        _log.Should().Equal(
            $"Pvp {Defender} {Attacker} 70 True",
            $"Trigger {Defender} {StatType.LowHPTemporaryHPPercent} 30",
            $"Attribute {Defender} {StatType.LowHPTemporaryHPPercent}",
            $"RefreshHealing {Attacker} {Defender}");
    }

    [TestCase(70, 60, true)]
    [TestCase(30, 60, false)]
    [TestCase(70, 40, false)]
    [TestCase(50, 60, false)]
    public void TheLowHPShieldBeforeFatalDamageNeedsAFatalHitFromAboveTheThreshold(int damage, int currentHP, bool grants)
    {
        _hitPoints[Defender] = (currentHP, 100);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 20);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 6);

        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, damage);
        Lines("Attribute").Should().HaveCount(grants ? 1 : 0);
    }

    [Test]
    public void TheLowHPShieldBeforeFatalDamageNeedsItsStatsALivingDefenderAndAReadyTrigger()
    {
        _hitPoints[Defender] = (60, 100);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 20);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 6);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 0);
        _invalid.Add(Gone);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Gone, 70);
        _dead.Add(Other);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Other, 70);
        _hitPoints[Defender] = (0, 100);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        _hitPoints[Defender] = (60, 0);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        _hitPoints[Defender] = (60, 100);
        _log.Should().BeEmpty();

        _triggerReady = false;
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        _log.Should().Equal($"Trigger {Defender} {StatType.LowHPTemporaryHPPercent} 0");
    }


    [Test]
    public void DamageOverTimeIsAdjustedByItsTypeAndReportsTheAdjustment()
    {
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 0, CombatDamageType.Physical, out var none).Should().Be(0);
        none.Should().Be(0);

        Stat(Defender, StatType.PhysicalDamageOverTimeTakenPercentAdjustment, -30);
        Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, -200);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Physical, out var physical).Should().Be(70);
        physical.Should().Be(-30);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Force, out var force)
            .Should().Be(CombatRules.ApplyPercentDamageAdjustment(100, -CombatRules.MaximumCombinedDamageReductionPercent));
        force.Should().Be(-CombatRules.MaximumCombinedDamageReductionPercent);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Fire).Should().Be(100);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Physical).Should().Be(70);
    }

    [Test]
    public void TheFatalDamageSaveSpendsItsTriggerAndGrantsTemporaryHPFromItsSource()
    {
        _hitPoints[Defender] = (20, 200);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);
        Stat(Defender, StatType.FatalDamageTemporaryHPCooldownSeconds, 60);
        Stat(Defender, StatType.FatalDamageTemporaryHPScalingAbilityScore, 20);
        _statusSources[StatType.FatalDamageTemporaryHPPercent] = Ally;

        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 20, false).Should().BeTrue();
        _log.Should().Equal(
            $"Trigger {Defender} {StatType.FatalDamageTemporaryHPPercent} 60",
            $"RemoveWithStat {Defender} {StatType.FatalDamageTemporaryHPPercent} False",
            $"Attribute {Ally} {StatType.FatalDamageTemporaryHPPercent}");
        Visuals().Last().Should().Be(VisualEffect.Vfx_Imp_Ac_Bonus);
        _engine.CallsTo("SetCurrentHitPoints").Should().BeEmpty();
    }

    [Test]
    public void ADyingDefenderIsStoodUpAtOneHPWhenTheSaveRestoresThem()
    {
        _recording = true;
        _hitPoints[Defender] = (0, 200);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);

        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, true).Should().BeTrue();
        _log.Should().Equal(
            $"Trigger {Defender} {StatType.FatalDamageTemporaryHPPercent} 0",
            $"RemoveWithStat {Defender} {StatType.FatalDamageTemporaryHPPercent} False",
            $"KeepLethal {Defender}",
            $"Attribute {Defender} {StatType.FatalDamageTemporaryHPPercent}");
        _engine.CallsTo("SetCurrentHitPoints").Should().ContainSingle().Which.Arguments[1].Should().Be(1);
    }

    [Test]
    public void ADyingDefenderIsStoodUpWithoutARecordingToo()
    {
        _hitPoints[Defender] = (0, 200);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);

        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, true).Should().BeTrue();
        Lines("KeepLethal").Should().BeEmpty();
        _engine.CallsTo("SetCurrentHitPoints").Should().ContainSingle();
    }

    [Test]
    public void TheFatalDamageSaveNeedsAValidDefenderItsStatsAFatalHitAndAReadyTrigger()
    {
        _invalid.Add(Gone);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Gone, 50, false).Should().BeFalse();
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, false).Should().BeFalse();
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();

        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);
        _hitPoints[Defender] = (60, 100);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();
        _hitPoints[Defender] = (0, 100);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();
        _hitPoints[Defender] = (20, 100);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, true).Should().BeFalse();
        _log.Should().BeEmpty();

        _triggerReady = false;
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();
        _log.Should().Equal($"Trigger {Defender} {StatType.FatalDamageTemporaryHPPercent} 0");
    }

    [TestCase("pc", true)]
    [TestCase("dm", true)]
    [TestCase("possessed", true)]
    [TestCase("hostile", true)]
    [TestCase("hostileBack", true)]
    [TestCase("enemy", true)]
    [TestCase("enemyBack", true)]
    [TestCase("neutral", false)]
    public void AHostileAttackSourceIsAPlayerADMOrAnyHostileOrEnemyRelation(string relation, bool hostile)
    {
        switch (relation)
        {
            case "pc": _players.Add(Attacker); break;
            case "dm": _dms.Add(Attacker); break;
            case "possessed": _possessed.Add(Attacker); break;
            case "hostile": _hostile.Add((Attacker, Defender)); break;
            case "hostileBack": _hostile.Add((Defender, Attacker)); break;
            case "enemy": _enemies.Add((Attacker, Defender)); break;
            case "enemyBack": _enemies.Add((Defender, Attacker)); break;
        }

        _service.IsHostileAttackSource(Defender, Attacker).Should().Be(hostile);
    }

    [TestCase(0, 20)]
    [TestCase(15, 35)]
    [TestCase(100, CombatDamageTakenService.MaximumGuardDamageReductionPercent)]
    [TestCase(-50, 0)]
    public void TheGuardReductionStartsAtItsBaseAndStaysWithinItsBounds(int adjustment, int expected)
    {
        Stat(Defender, StatType.GuardDamageReductionPercentAdjustment, adjustment);

        _service.GetGuardDamageReductionPercent(Defender).Should().Be(expected);
        CombatDamageTakenService.BaseGuardDamageReductionPercent.Should().Be(20);
    }

    [Test]
    public void TheForceDefenseBlendFollowsTheConversionPercentAndReadsForceDefenseOnlyWhenItConverts()
    {
        var reads = 0;
        int Force()
        {
            reads++;
            return 20;
        }

        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Fire, 100, Force).Should().Be(100);
        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, Force).Should().Be(100);
        reads.Should().Be(0);

        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 25);
        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, Force).Should().Be((100 * 75 + 20 * 25) / 100);
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 150);
        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, Force).Should().Be(20);
        reads.Should().Be(2);
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheContainerBuildsThePart()
    {
        var part = _facades.Get<ICombatDamageTakenService>();
        part.Should().BeOfType<CombatDamageTakenService>();
        CombatDamageTakenService.BaseGuardDamageReductionPercent.Should().Be(CombatDamageTakenService.BaseGuardDamageReductionPercent);
        CombatDamageTakenService.MaximumGuardDamageReductionPercent.Should().Be(CombatDamageTakenService.MaximumGuardDamageReductionPercent).And.Be(55);
    }
}
