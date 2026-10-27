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
        death.Should().Contain("SpaceDeathRules.ModulesDrop(IsWildSpace(GetArea(creature)), killerIsPlayer)");
        death.Should().Contain("if (modulesDrop)");
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
