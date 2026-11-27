using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the engine glue of the death and PvP update (W2d) that the pure rules tests cannot reach.
/// </summary>
public class SpaceDeathWiringTests
{
    [Test]
    public void ShipDamage_IsGatedAndRampedByThePvpRules()
    {
        var space = ReadServerSource("Service", "Space.cs");

        // Both damage entry points refuse a forbidden hit and apply the ramp.
        space.Split("if (!CanShipAttack(attacker, target))").Length.Should().Be(3);
        space.Split("amount = ApplyPvpDamageRamp(attacker, target, amount);").Length.Should().Be(3);
        space.Split("RecordShipKiller(target, attacker);").Length.Should().Be(3);

        var pvp = ReadServerSource("Service", "SpacePvp.cs");
        pvp.Should().Contain("PvpCombat.RecordHit(attacker, target);");
        pvp.Should().Contain("SpacePvpRules.CanAttack(true, GetIsReactionTypeHostile(target, attacker))");
        pvp.Should().NotContain("GetPVPSetting", "space defers to the engine instead of reading the PvP setting itself");
    }

    [Test]
    public void DeferredShipDeaths_DestroyTheShipOnlyOnce()
    {
        // A shot's main hit and its ExtraDamage rider can each queue a death before either runs; the first ejects the
        // pilot alive, so a later one must not kill the revived player on the ground.
        var space = ReadServerSource("Service", "Space.cs");
        var helper = space.Substring(space.IndexOf("private static void DestroyShipIfStillWrecked(uint target)", StringComparison.Ordinal));
        helper = helper.Substring(0, helper.IndexOf("\n        }\n", StringComparison.Ordinal));

        helper.Should().Contain("if (!GetIsObjectValid(target) || GetIsDead(target))");
        helper.Should().Contain("if (!IsPlayerInSpaceMode(target))");
        helper.Should().Contain("else if (GetShipStatus(target) is { Hull: > 0 })");
        space.Split("DestroyShipIfStillWrecked(target)").Length.Should().Be(3, "both ship damage paths defer through the check");
        space.Should().Contain("AssignCommand(OBJECT_SELF, () => DestroyShipIfStillWrecked(creature));");
        space.Should().NotContain("ApplyEffectToObject(DurationType.Instant, EffectDeath(), target));");
    }

    [Test]
    public void EnemyFinders_UseTheShipEnemyTest()
    {
        ReadServerSource("Service", "SpaceThreat.cs").Should().Contain("!IsShipEnemy(source, nearby)");
        ReadServerSource("Service", "SpaceAbilities.cs").Should().Contain("Space.IsShipEnemy(activator, primaryTarget)");
        ReadServerSource("Service", "SpaceAbilities.cs").Should().Contain("Space.GetPvpRefusal(ship, target)");
        ReadServerSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs").Should().Contain("Space.IsShipHostile(activator, target)");
        ReadServerSource("Service", "SpaceToggles.cs").Should().Contain("!CanShipAttack(creature, target)");
    }

    [Test]
    public void PlayerDeath_RunsTheEscapePodsBeforeTheGroundDeathFlow()
    {
        var death = ReadServerSource("Service", "Death.cs");
        var pods = death.IndexOf("Space.ApplyDeath(player)", StringComparison.Ordinal);
        var panel = death.IndexOf("PopUpDeathGUIPanel", StringComparison.Ordinal);

        pods.Should().BeGreaterThan(0);
        pods.Should().BeLessThan(panel);
        death.Should().Contain("public static int ApplyPenalties(uint player)");

        // The old module-death handler on Space would race the death panel, so it must stay gone.
        ReadServerSource("Service", "SpaceDeath.cs").Should().NotContain("NWNEventHandler(ScriptName.OnModuleDeath)");
        ReadServerSource("Service", "Space.cs").Should().NotContain("public static void ApplyDeath()");
    }

    [Test]
    public void EscapePods_ChargeTheGroundDebtAndKeepModulesOutsideWildSpace()
    {
        var death = ReadServerSource("Service", "SpaceDeath.cs");

        death.Should().Contain("Death.ApplyPenalties(rider)");
        death.Should().Contain("SpaceDeathRules.ModulesDrop(IsWildSpace(GetArea(creature)), pvpStakes)");
        death.Should().Contain("if (modulesDrop)");
    }

    [Test]
    public void EscapePods_LeaveOutThePlayersReturnedToDockedShipsAndFindPassengersByArea()
    {
        var death = ReadServerSource("Service", "SpaceDeath.cs");

        death.IndexOf("DetachDockedShips(creature, wreckedRiders)", StringComparison.Ordinal)
            .Should().BeGreaterThan(0)
            .And.BeLessThan(death.IndexOf("TakeShipKiller(creature)", StringComparison.Ordinal));
        death.Should().Contain("!returnedToDockedShips.Contains(x.Key)");
        death.Should().Contain("GetPlayersInArea(instance.Area)");
        death.Should().Contain("!returnedToDockedShips.Contains(x)");
        death.Should().Contain("ReturnDroidCrew(dbPlayerShip, \"was destroyed\")");
    }

    [Test]
    public void PvpLogout_DetachesDockedShipsAtTheWreckBeforeTowingTheHost()
    {
        var death = ReadServerSource("Service", "SpaceDeath.cs");
        var start = death.IndexOf("private static void ForfeitShipOnPvpLogout(uint player)", StringComparison.Ordinal);
        var end = death.IndexOf("public static void ApplyPendingShipLoss()", StringComparison.Ordinal);
        start.Should().BeGreaterThan(0);
        var body = death.Substring(start, end - start);

        // The same visitor handling as a destruction, while the pilot still stands at the fight and before the
        // host's position is cleared (which sends it, and anything still aboard, to its home dock).
        var detach = body.IndexOf("DetachDockedShips(player,", StringComparison.Ordinal);
        detach.Should().BeGreaterThan(0);
        detach.Should().BeLessThan(body.IndexOf("dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition)", StringComparison.Ordinal));
        detach.Should().BeLessThan(body.IndexOf("DB.Set(dbProperty)", StringComparison.Ordinal));

        // The logout handler forfeits the ship while the pilot is still in space mode, which the detach requires.
        var space = ReadServerSource("Service", "Space.cs");
        space.IndexOf("ForfeitShipOnPvpLogout(player);", StringComparison.Ordinal)
            .Should().BeGreaterThan(0)
            .And.BeLessThan(space.IndexOf("_playersInSpace.Remove(player);", StringComparison.Ordinal));
    }

    [Test]
    public void PropertyInstancePlayers_AreAddedOnTheEnteringPlayer()
    {
        var property = ReadServerSource("Service", "Property.cs");
        var start = property.IndexOf("public static void EnterPropertyInstance()", StringComparison.Ordinal);
        var end = property.IndexOf("public static void ExitPropertyInstance()", StringComparison.Ordinal);
        var body = property.Substring(start, end - start);

        body.Should().Contain("GetEnteringObject()");
        body.Should().NotContain("GetExitingObject()");
    }

    [Test]
    public void RepairBill_UsesTheHullMultiplier()
    {
        var view = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs");

        view.Should().Contain("ShipRepairBill.GetSizeMultiplier(detail?.Hull, ship.Status.CapitalShip)");
        view.Should().Contain("ShipRepairBill.Calculate(");
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
