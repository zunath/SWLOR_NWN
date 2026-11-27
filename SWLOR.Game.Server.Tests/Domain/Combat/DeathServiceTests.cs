using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The death service: dying, the death handler (reputation reset, subdual, the respawn panel, the debt waiver) and the respawn handler,
/// which sends the player home with the XP debt penalty through the respawn part. The engine is a recording
/// fake with real local variables, the database an in-memory one, the neighbours scripted proxies, and the unconverted
/// combat, ability, status effect, recorder, log and feature calls are answered by a subclass.
/// </summary>
[TestFixture]
public class DeathServiceTests : DeathPartsTestBase
{
    [Test]
    public void OnPlayerDying_KillsThePlayerWhenNothingPreventsIt()
    {
        _death.OnPlayerDying();

        _death.Calls.Should().Contain("TryPreventFatalDamage:1001:0:True");
        _engine.CallsTo("EffectDeath").Should().HaveCount(1);
        _engine.CallsTo("ApplyEffectToObject").Should().HaveCount(1);
        _engine.CallsTo("ApplyEffectToObject")[0].Arguments[2].Should().Be(Player);
    }

    [Test]
    public void OnPlayerDying_DoesNothingWhenFatalDamageIsPrevented()
    {
        _death.PreventFatal = true;

        _death.OnPlayerDying();

        _engine.CountOf("EffectDeath").Should().Be(0);
        _engine.CountOf("ApplyEffectToObject").Should().Be(0);
    }

    [Test]
    public void OnPlayerDeath_ByAnNpcShowsTheRespawnPanelAndWritesTheAudit()
    {
        _death.OnPlayerDeath();

        _animation.Calls.Should().ContainSingle(call => call.Member == "ClearOnDeath" && (uint)call.Arguments[0]! == Player);
        _death.Calls.Should().ContainInOrder("ClearQueuedAbility:1001", "RecordPlayerDeath:1001:2002", "ClearStatusEffectsOnDeath:1001");
        _engine.CallsTo("SetStandardFactionReputation").Select(call => (StandardFaction)call.Arguments[0]!)
            .Should().Equal(StandardFaction.Commoner, StandardFaction.Merchant, StandardFaction.Defender);
        _engine.CallsTo("SetStandardFactionReputation").Should().OnlyContain(call => (int)call.Arguments[1]! == 100);
        var panel = _engine.CallsTo("PopUpDeathGUIPanel").Single();
        panel.Arguments[0].Should().Be(Player);
        ((string)panel.Arguments[4]!).Should().StartWith("You have died.");
        _death.Logs.Should().ContainSingle()
            .Which.Should().Be((LogGroup.Death, "DEATH: audit-1001 - Area Name - area_tag - other Killed by: audit-2002"));
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void OnPlayerDeath_ClearsThePersonalReputationOfEveryHostileFactionMember()
    {
        _engine.SetReturnSequence("GetFirstFactionMember", 31u);
        _engine.SetReturnSequence("GetNextFactionMember", 32u, 33u);

        _death.OnPlayerDeath();

        _engine.CallsTo("ClearPersonalReputation").Select(call => (uint)call.Arguments[1]!).Should().Equal(31u, 32u, 33u);
    }

    [Test]
    public void OnPlayerDeath_ClaimsTheFirstDebtWaiverThatCanBeConsumed()
    {
        var spent = new TestWaiver(consumes: false);
        var good = new TestWaiver(consumes: true);
        var unused = new TestWaiver(consumes: true);
        _death.Waivers.AddRange(new[] { spent, good, unused });

        _death.OnPlayerDeath();

        spent.Asked.Should().BeTrue();
        good.Asked.Should().BeTrue();
        unused.Asked.Should().BeFalse();
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(1);
    }

    [Test]
    public void OnPlayerDeath_ReplacesAPreviousClaimWhenNoWaiverIsLeft()
    {
        _engine.SetLocalInt(Player, "DEATH_DEBT_WAIVED", 1);

        _death.OnPlayerDeath();

        _engine.CallsTo("DeleteLocalInt").Should().Contain(call => (string)call.Arguments[1]! == "DEATH_DEBT_WAIVED");
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(0);
    }

    [Test]
    public void OnPlayerDeath_ByAPlayerWithoutSubdualModeShowsTheRespawnPanelAndNoSubdualMessages()
    {
        _hostileIsPlayer = true;

        _death.OnPlayerDeath();

        // A lethal kill by another player leaves the player dead with the respawn panel, like any other death.
        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(1);
        _engine.CountOf("SendMessageToPC").Should().Be(0);
        _death.Calls.Should().NotContain(call => call.StartsWith("ApplyStatusEffect"));
    }

    [Test]
    public void OnPlayerDeath_ByADMOrAPossessedPlayerIsAnOrdinaryDeath()
    {
        _hostileIsPlayer = true;
        _hostileIsDm = true;
        _death.OnPlayerDeath();
        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(1);

        _engine.ClearCalls();
        _hostileIsDm = false;
        _hostilePossessed = true;
        _death.OnPlayerDeath();
        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(1);
    }

    [Test]
    public void OnPlayerDeath_ASubduedPlayerIsRevivedAndPenalizedWithoutDebtOrPanel()
    {
        _hostileIsPlayer = true;
        _db.Get<Player>("uuid-2002").Settings.IsSubdualModeEnabled = true;
        _death.Recording = true;

        _death.OnPlayerDeath();

        _death.Calls.Should().Contain("RecordSubdual:1001:2002");
        _engine.CallsTo("SendMessageToPC").Single().Arguments[1].Should().Be("You have been subdued.");
        var nearby = _messaging.Calls.Single(call => call.Member == "SendMessageNearbyToPlayers");
        ((uint)nearby.Arguments[0]!).Should().Be(Player);
        var build = (BuildMessageDelegate)nearby.Arguments[1]!;
        build(77).Should().Be("name77-1001 has been subdued by name77-2002.");
        _engine.CountOf("EffectResurrection").Should().Be(1);
        _death.Calls.Should().Contain("RestoreGhostAfterRevive:1001");
        _engine.CallsTo("DelayCommand").Single().Arguments[0].Should().Be(0.1f);
        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!)();
        _death.Calls.Should().Contain("ReapplyAura:1001");
        _death.Calls.Should().Contain($"ApplyStatusEffect:1001:1001:{nameof(KnockdownStatusEffect)}:60");
        _death.Calls.Should().Contain($"ApplyStatusEffect:1001:1001:{nameof(SubdualPenaltyStatusEffect)}:300");
        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(0);
        _death.Calls.Should().NotContain("RecordDeathDebtWaiver");
    }

    [Test]
    public void OnPlayerDeath_ASubduedPlayerDoesNotRecordWhenNotRecordingAndForgetsAnOldWaiver()
    {
        _hostileIsPlayer = true;
        _db.Get<Player>("uuid-2002").Settings.IsSubdualModeEnabled = true;
        _death.Waivers.Add(new TestWaiver(consumes: true));

        _death.OnPlayerDeath();

        _death.Calls.Should().NotContain(call => call.StartsWith("RecordSubdual"));
        _death.Waivers[0].Asked.Should().BeFalse();
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(0);
    }

    [Test]
    public void OnPlayerRespawn_HealsHalfOfMaximumAndSendsThePlayerHomeWithThePenalty()
    {
        Record.RespawnAreaResref = "med_center";
        Record.RespawnLocationX = 1f;
        Record.RespawnLocationY = 2f;
        Record.RespawnLocationZ = 3f;
        _death.Recording = true;

        _death.OnPlayerRespawn();

        _engine.CallsTo("EffectHeal").Single().Arguments[0].Should().Be(45);
        _engine.CountOf("EffectResurrection").Should().Be(1);
        _death.Calls.Should().ContainInOrder("RestoreGhostAfterRevive:1001", "RecordRespawn:1001", "BeginRespawnAttribution", "EndRespawnAttribution");
        var assign = _engine.CallsTo("AssignCommand").Single();
        assign.Arguments[0].Should().Be(Player);
        _engine.ClearCalls();
        ((Action)assign.Arguments[1]!)();
        _engine.CallsTo("ClearAllActions").Single().Arguments[0].Should().Be(true);
        _engine.CountOf("JumpToLocation").Should().Be(1);
        _death.Logs.Should().ContainSingle().Which.Should().Be((LogGroup.Death, "RESPAWN - audit-1001 - 150 XP debt"));
    }

    [Test]
    public void OnPlayerRespawn_DoesNotRecordWhenNotRecording()
    {
        _death.OnPlayerRespawn();

        _death.Calls.Should().NotContain(call => call.StartsWith("RecordRespawn"));
        _death.Calls.Should().Contain("BeginRespawnAttribution");
    }

    [TestCase(0, 15)]
    [TestCase(49, 15)]
    [TestCase(50, 25)]
    [TestCase(199, 25)]
    [TestCase(200, 35)]
    [TestCase(299, 35)]
    [TestCase(300, 45)]
    [TestCase(900, 45)]
    public void OnPlayerRespawn_ChargesDebtByTheSkillPointsAcquired(int skillPoints, int multiplier)
    {
        Record.TotalSPAcquired = skillPoints;
        _engine.SetReturn("GetAbilityScore", 10);

        _death.OnPlayerRespawn();

        Record.XPDebt.Should().Be(skillPoints * multiplier);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!)
            .Should().Contain($"{skillPoints * multiplier} XP added to your debt. (Total: {skillPoints * multiplier} XP)");
    }

    [Test]
    public void OnPlayerRespawn_ReducesTheDebtByMedicalCenterLevelAndSocial()
    {
        Record.TotalSPAcquired = 100;
        Record.CitizenPropertyId = "city-1";
        _property.On("GetEffectiveUpgradeLevel", args =>
        {
            args[0].Should().Be("city-1");
            args[1].Should().Be(PropertyUpgradeType.MedicalCenterLevel);
            return 2;
        });
        _engine.SetReturn("GetAbilityScore", 15);

        _death.OnPlayerRespawn();

        // 100 * 25 = 2500; reduction 2 * 5% + 5 * 3% = 25%.
        Record.XPDebt.Should().Be(2500 - (int)(2500 * (2 * 0.05f + 5 * 0.03f)));
    }

    [Test]
    public void OnPlayerRespawn_CapsTheReductionAtEightyPercentAndTheDebtAtItsMaximum()
    {
        Record.TotalSPAcquired = 100;
        _property.On("GetEffectiveUpgradeLevel", _ => 10);
        _engine.SetReturn("GetAbilityScore", 40);
        _death.OnPlayerRespawn();
        Record.XPDebt.Should().Be(2500 - (int)(2500 * 0.8f));

        Record.XPDebt = 9999990;
        Record.TotalSPAcquired = 400;
        _property.On("GetEffectiveUpgradeLevel", _ => 0);
        _engine.SetReturn("GetAbilityScore", 5);
        _death.OnPlayerRespawn();
        Record.XPDebt.Should().Be(9999999);
    }

    [Test]
    public void OnPlayerRespawn_AClaimedWaiverSparesTheDebtOnce()
    {
        Record.XPDebt = 500;
        _engine.SetLocalInt(Player, "DEATH_DEBT_WAIVED", 1);

        _death.OnPlayerRespawn();

        Record.XPDebt.Should().Be(500);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).Should().Contain("Requiem spares you any XP debt.");
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(0);
        _death.Logs.Should().ContainSingle().Which.Should().Be((LogGroup.Death, "RESPAWN - audit-1001 - 500 XP debt"));
    }

    [Test]
    public void A_revive_in_place_tells_every_revived_listener_in_order()
    {
        // The subdual revive used to call the ability service's aura reapply; the death service now tells each listener the
        // container registered (the aura part), once per revive.
        var heard = new List<string>();
        var death = new ReviveProbe(new[] { new RecordingRevivedListener("first", heard), new RecordingRevivedListener("second", heard) });

        death.Revive(42);

        heard.Should().Equal("first:42", "second:42");
    }

    private sealed class RecordingRevivedListener : ICreatureRevivedListener
    {
        private readonly string _name;
        private readonly List<string> _heard;

        public RecordingRevivedListener(string name, List<string> heard)
        {
            _name = name;
            _heard = heard;
        }

        public void OnCreatureRevived(uint creature) => _heard.Add($"{_name}:{creature}");
    }

}
