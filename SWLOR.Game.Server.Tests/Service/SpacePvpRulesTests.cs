using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PvpCombatService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class SpacePvpRulesTests
{
    [Test]
    public void Attack_OnNpcShip_IsNeverStoppedByPvpRules()
    {
        var decision = SpacePvpRules.CanAttack(false, false, PvPSetting.NoPvP, false);

        decision.Should().Be(SpacePvpDecision.NotPlayerVersusPlayer);
        SpacePvpRules.IsAllowed(decision).Should().BeTrue();
    }

    [Test]
    public void Attack_OnPlayerNotMarkedHostile_IsRefused()
    {
        var decision = SpacePvpRules.CanAttack(true, false, PvPSetting.FullPvP, false);

        decision.Should().Be(SpacePvpDecision.NotHostile);
        SpacePvpRules.IsAllowed(decision).Should().BeFalse();
        SpacePvpRules.GetRefusal(decision).Should().Contain("hostile");
    }

    [Test]
    public void Attack_OnHostilePlayerInFullPvpArea_IsAllowed()
    {
        var decision = SpacePvpRules.CanAttack(true, true, PvPSetting.FullPvP, true);

        decision.Should().Be(SpacePvpDecision.Allowed);
        SpacePvpRules.IsAllowed(decision).Should().BeTrue();
        SpacePvpRules.GetRefusal(decision).Should().BeEmpty();
    }

    [Test]
    public void Attack_OnHostilePlayerInNoPvpArea_IsRefused()
    {
        var decision = SpacePvpRules.CanAttack(true, true, PvPSetting.NoPvP, false);

        decision.Should().Be(SpacePvpDecision.AreaForbids);
        SpacePvpRules.IsAllowed(decision).Should().BeFalse();
        SpacePvpRules.GetRefusal(decision).Should().Contain("area");
    }

    [Test]
    public void Attack_InPartyPvpArea_ProtectsOnlyThePartyMembers()
    {
        SpacePvpRules.CanAttack(true, true, PvPSetting.PartyPvP, false).Should().Be(SpacePvpDecision.Allowed);
        SpacePvpRules.CanAttack(true, true, PvPSetting.PartyPvP, true).Should().Be(SpacePvpDecision.PartyProtected);
        SpacePvpRules.GetRefusal(SpacePvpDecision.PartyProtected).Should().Contain("party");
    }

    [Test]
    public void Attack_InUnresolvedServerDefaultArea_IsRefused()
    {
        // The caller resolves ServerDefault first; an unresolved value must not open PvP by accident.
        SpacePvpRules.CanAttack(true, true, PvPSetting.ServerDefault, false).Should().Be(SpacePvpDecision.AreaForbids);
    }

    [TestCase(PvPSetting.ServerDefault, PvPSetting.FullPvP, PvPSetting.FullPvP)]
    [TestCase(PvPSetting.ServerDefault, PvPSetting.NoPvP, PvPSetting.NoPvP)]
    [TestCase(PvPSetting.NoPvP, PvPSetting.FullPvP, PvPSetting.NoPvP)]
    [TestCase(PvPSetting.PartyPvP, PvPSetting.NoPvP, PvPSetting.PartyPvP)]
    public void ResolveAreaSetting_UsesTheServerSettingOnlyForDefaultAreas(PvPSetting area, PvPSetting server, PvPSetting expected)
    {
        SpacePvpRules.ResolveAreaSetting(area, server).Should().Be(expected);
    }

    [Test]
    public void SupportHostility_CoversEnemiesAndPlayersEitherSideMarked()
    {
        // NPC ships keep the engine reaction.
        SpacePvpRules.IsHostile(false, true, false, false).Should().BeTrue();
        SpacePvpRules.IsHostile(false, false, false, false).Should().BeFalse();

        // Two players: a mark in either direction blocks repair and orders; no mark leaves them friendly.
        SpacePvpRules.IsHostile(true, false, true, false).Should().BeTrue();
        SpacePvpRules.IsHostile(true, false, false, true).Should().BeTrue();
        SpacePvpRules.IsHostile(true, false, false, false).Should().BeFalse();
    }

    [Test]
    public void Ramp_RaisesDamageOnlyBetweenPlayers()
    {
        // 180 s into PvP combat the ground ramp is +20%, and at 300 s it is +100%.
        var at180 = PvpCombatRules.GetStepPercent(PvpCombatRules.DamageIncreaseSteps, 180);
        var at300 = PvpCombatRules.GetStepPercent(PvpCombatRules.DamageIncreaseSteps, 300);

        SpacePvpRules.ApplyRamp(100, true, at180).Should().Be(120);
        SpacePvpRules.ApplyRamp(100, true, at300).Should().Be(200);
        SpacePvpRules.ApplyRamp(100, true, 0).Should().Be(100);
        SpacePvpRules.ApplyRamp(100, false, at300).Should().Be(100, "NPC ships never get the player ramp");
    }

    [Test]
    public void Ramp_RoundsTheIncreaseUpLikeGroundDamage()
    {
        SpacePvpRules.ApplyRamp(7, true, 20).Should().Be(PvpCombatRules.IncreaseDamage(7, 20));
        SpacePvpRules.ApplyRamp(7, true, 20).Should().Be(9);
        SpacePvpRules.ApplyRamp(0, true, 100).Should().Be(0);
    }

    [Test]
    public void HardCrowdControl_AgainstPlayers_IsHalvedBySharedRules()
    {
        // Ship statuses go through StatusEffect, which applies the ground halving to a player target.
        PvpCombatRules.ShortenHardCrowdControlTicks(10).Should().Be(5);
    }

    [Test]
    public void Toggle_OnAPlayerTheRulesNoLongerAllow_SwitchesOffWithAMessage()
    {
        var check = new ShipToggleCheck { TargetValid = true, PvpForbidden = true, Capacitor = 50, RequiredCapacitor = 5 };

        var reason = ShipToggleRules.Evaluate(check);

        reason.Should().Be(ShipToggleAutoOffReason.PvpForbidden);
        ShipToggleRules.AutoOffMessage(reason, "Laser Cannon", string.Empty).Should().Contain("can no longer attack");
    }

    [Test]
    public void Toggle_OnAllowedTarget_KeepsFiring()
    {
        var check = new ShipToggleCheck { TargetValid = true, PvpForbidden = false, Capacitor = 50, RequiredCapacitor = 5 };

        ShipToggleRules.Evaluate(check).Should().Be(ShipToggleAutoOffReason.None);
    }
}
