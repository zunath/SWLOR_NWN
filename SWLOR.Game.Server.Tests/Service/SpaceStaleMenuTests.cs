using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Space menus act on the current state, never on the copy a window or page read when it opened: each choice reads
/// the record again and checks what it needs (the ship is still docked here, the module still in its slot, the
/// permission still held) before it saves.
/// </summary>
public class SpaceStaleMenuTests
{
    private static string ShipManagement() =>
        ReadServer("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs");

    [Test]
    public void ShipManagement_RefitAndDockChecks_ReadTheShipAsSavedNow()
    {
        var docked = Between(ShipManagement(), "private PlayerShip GetShipDockedHere(string shipId)", "private PlayerShip GetShipToRefit(string shipId)");
        Index(docked, "var dbShip = DB.Get<PlayerShip>(shipId);")
            .Should().BeLessThan(Index(docked, "GetShipLocation(dbShip, dbProperty, out _) == GetArea(Player)"));

        var refit = Between(ShipManagement(), "private PlayerShip GetShipToRefit(string shipId)", "private void RefreshShipIfOpen(");
        Index(refit, "var dbShip = GetShipDockedHere(shipId);")
            .Should().BeLessThan(Index(refit, "access.Permissions.GetValueOrDefault(PropertyPermissionType.RefitShip)"),
                "the Refit Ship permission can be revoked while the window is open");
    }

    [Test]
    public void ShipManagement_UninstallingAModule_ActsOnTheSlotAsSavedNow()
    {
        var power = Between(ShipManagement(), "private void ProcessPower(ShipModulePowerType row, int slot)", "private void ProcessConfiguration(int slot)");
        AssertUninstallReadsAgain(power[Index(power, "Would you like to uninstall it?")..],
            "var currentModules = GetPowerModules(current.Status, row);", "currentModules.Remove(slot);");

        var config = Between(ShipManagement(), "private void ProcessConfiguration(int slot)", "public Action OnClickHighPower1()");
        AssertUninstallReadsAgain(config[Index(config, "Would you like to uninstall it?")..],
            "current.Status.ConfigurationModules.TryGetValue(slot, out var fitted)", "current.Status.ConfigurationModules.Remove(slot);");
    }

    private static void AssertUninstallReadsAgain(string modal, string slotRead, string removal)
    {
        var reread = Index(modal, "var current = GetShipToRefit(shipId);");
        Index(modal, slotRead).Should().BeGreaterThan(reread);
        Index(modal, "fitted.SerializedItem != module.SerializedItem")
            .Should().BeLessThan(Index(modal, "Item.TryAcquireItem(Player, item)"), "a module already taken out is never handed out twice");
        Index(modal, "ObjectPlugin.Deserialize(fitted.SerializedItem)").Should().BeGreaterThan(reread);
        Index(modal, removal).Should().BeLessThan(Index(modal, "DB.Set(current);"));
        modal[..Index(modal, "LoadShip();\n                });")].Should().NotContain("DB.Set(dbShip);", "the copy read when the slot was clicked is never saved");
    }

    [Test]
    public void ShipManagement_InstallingAModule_NeverOverwritesAModuleFittedSince()
    {
        var power = Between(ShipManagement(), "private void ProcessPower(ShipModulePowerType row, int slot)", "private void ProcessConfiguration(int slot)");
        Index(power, "dbShip = GetShipToRefit(shipId);")
            .Should().BeLessThan(Index(power, "if (modules.ContainsKey(slot))"));
        Index(power, "if (modules.ContainsKey(slot))").Should().BeLessThan(Index(power, "modules[slot] = new ShipStatus.ShipStatusModule"));

        var config = Between(ShipManagement(), "private void ProcessConfiguration(int slot)", "public Action OnClickHighPower1()");
        Index(config, "dbShip = GetShipToRefit(shipId);")
            .Should().BeLessThan(Index(config, "if (dbShip.Status.ConfigurationModules.ContainsKey(slot))"));
        Index(config, "if (dbShip.Status.ConfigurationModules.ContainsKey(slot))")
            .Should().BeLessThan(Index(config, "dbShip.Status.ConfigurationModules[slot] = new ShipStatus.ShipStatusModule"));
    }

    [Test]
    public void ShipManagement_RefitRepairUnregisterAndBoard_RecheckTheShip()
    {
        var refit = Between(ShipManagement(), "private void CompleteRefit(", "private void SetMidSlot(");
        Index(refit, "var dbShip = GetShipToRefit(shipId);").Should().BeLessThan(Index(refit, "CreditPayments.Take(player, mark);"));

        var repair = Between(ShipManagement(), "public Action OnClickRepair()", "private void CompleteRepair(");
        var modal = repair[Index(repair, "ShowModal(")..];
        Index(modal, "if (GetShipDockedHere(shipId) == null)").Should().BeLessThan(Index(modal, "AssignCommand(player, () => CompleteRepair(player, shipId, price));"));
        var completeRepair = Between(ShipManagement(), "private void CompleteRepair(", "dbShip.Status.Hull = dbShip.Status.MaxHull;");
        Index(completeRepair, "var dbShip = GetShipDockedHere(shipId);").Should().BeLessThan(Index(completeRepair, "if (CalculateRepairBill(dbShip) != price)"));

        var unregister = Between(ShipManagement(), "public Action OnClickUnregisterShip()", "Space.RemoveUnregisteredShip(shipId);");
        Index(unregister, "var dbShip = GetShipDockedHere(shipId);").Should().BeLessThan(Index(unregister, "if (dbProperty.OwnerPlayerId != playerId)"));
        Index(unregister, "if (dbProperty.OwnerPlayerId != playerId)")
            .Should().BeLessThan(Index(unregister, "var deed = ObjectPlugin.Deserialize(dbShip.SerializedItem);"),
                "a ship sold or given away while the prompt was open is never turned back into its former owner's deed");

        var board = Between(ShipManagement(), "public Action OnClickBoardShip()", "public Action OnClickRemoveAccess()");
        Index(board, "var dbShip = GetShipDockedHere(shipId);").Should().BeLessThan(Index(board, "Property.TryResolveEnterableInstance(Player, dbShip.PropertyId, out var instance)"),
            "a ship that launched since it was selected cannot be boarded from the dock");

        var cancel = Between(ShipManagement(), "ShowModal(\"Cancel this ship's market listing?\"", "FloatingTextStringOnCreature(\"Listing cancelled.\"");
        Index(cancel, "DB.Get<PlayerShip>(shipId)?.OwnerPlayerId != GetObjectUUID(Player)")
            .Should().BeLessThan(Index(cancel, "ShipTransfer.RemoveListings(shipId);"), "a new owner's listing is never cancelled by the old owner's prompt");
    }

    [Test]
    public void StationBuildingMenus_RecheckPermissionsAndStations()
    {
        var dialog = ReadServer("Feature", "DialogDefinition", "StationBuildingDialog.cs");

        var collect = Between(dialog, "page.AddResponse(\"Collect the ore\"", "page.AddResponse(\"Assigned droid\"");
        Index(collect, "Property.HasPropertyPermission(player, station.Id, PropertyPermissionType.AccessTreasury)")
            .Should().BeLessThan(Index(collect, "StationBuildings.CollectDrill(player, station, state.BuildingId);"));

        var recall = Between(dialog, "page.AddResponse($\"Recall {state.DroidName}\"", "page.AddResponse(\"Assign one of your droids\"");
        Index(recall, "var current = StationBuildings.GetState(building);")
            .Should().BeLessThan(Index(recall, "current.DroidOwnerPlayerId != GetObjectUUID(player) && !CanManageStructures(station)"));
        Index(recall, "!CanManageStructures(station)").Should().BeLessThan(Index(recall, "StationBuildings.RecallDroid(player, building);"),
            "a droid posted since by someone else is recalled only by a structure manager");

        var assign = Between(dialog, "private void DroidPickPageInit(", "// The Jump Beacon");
        Index(assign, "if (!CanManageStructures(station))").Should().BeLessThan(Index(assign, "StationBuildings.AssignDroid(player, building, chosen)"));

        var jump = Between(dialog, "page.AddResponse(\"CONFIRM JUMP\"", "private void AllyPageInit(");
        Index(jump, "var origin = DB.Get<WorldProperty>(stationId);").Should().BeLessThan(Index(jump, "StationBuildings.Jump(player, origin, target, ship.Id)"));
        Index(jump, "var target = DB.Get<WorldProperty>(destinationId);").Should().BeLessThan(Index(jump, "StationBuildings.Jump(player, origin, target, ship.Id)"));
        jump.Should().Contain("target.IsQueuedForDeletion").And.NotContain("StationBuildings.Jump(player, station, destination");
    }

    [Test]
    public void StarportDocking_RefusesAHoldOrBlockade_BeforeSavingTheDock()
    {
        var dialog = ReadServer("Feature", "DialogDefinition", "StarportDockDialog.cs");
        var choice = Between(dialog, "page.AddResponse(dockName, () =>", "Space.WarpPlayerInsideShip(player);");
        var firstSave = Index(choice, "DB.Set(");
        Index(choice, "Space.RefuseWhileInterdicted(player)").Should().BeLessThan(firstSave,
            "an interdicted ship must not be recorded as docked while its pilot stays in space");
        Index(choice, "Space.GetIncursionDockRefusal(dock)").Should().BeLessThan(firstSave,
            "a blockade that took the dock while the menu was open stops the docking");
    }

    [Test]
    public void CityTradeExchange_ReadsTheDockAgainWhenChosen()
    {
        var dialog = ReadServer("Feature", "DialogDefinition", "StarportDialog.cs");
        var city = dialog[Index(dialog, "private void AddCityTradeExchangeResponse(")..];
        var choice = city[Index(city, "page.AddResponse(\"Trade Exchange\"")..];
        Index(choice, "dock = Trade.TryGetCityDock(starportId);").Should().BeLessThan(Index(choice, "if (!dock.HasExchange)"));
    }

    [Test]
    public void CityUpgrades_RecheckTheManageUpgradesPermission()
    {
        // Facility upgrades start City Projects. The confirmation calls Start, which checks the power again before escrow takes the credits.
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.Projects.cs");
        var startProject = Between(viewModel, "private void StartProject(CityProjectType type)", "public Action StartHomeGround()");
        var modal = startProject[Index(startProject, "ShowModal(")..];
        Index(modal, "CityProjects.Start(_cityId, type, GetObjectUUID(Player), out var project)").Should().BeGreaterThanOrEqualTo(0);

        var projects = ReadServer("Service", "CityProjects.cs");
        var check = Between(projects, "public static CityProjectRefusal CheckStart(", "public static CityProjectRefusal Start(");
        Index(check, "CityPowers.Can(city, playerId, CityPower.StartProjects)").Should().BeGreaterThanOrEqualTo(0);
        var start = projects[Index(projects, "public static CityProjectRefusal Start(")..];
        Index(start, "var refusal = CheckStart(city, type, playerId);").Should().BeLessThan(Index(start, "CityTreasury.PostEscrow("),
            "the confirmation runs the permission check again before the treasury is spent");
    }

    private static int Index(string source, string text)
    {
        var index = source.IndexOf(text, StringComparison.Ordinal);
        index.Should().BeGreaterThanOrEqualTo(0, $"the source should contain: {text}");
        return index;
    }

    private static string Between(string source, string start, string end)
    {
        var from = Index(source, start);
        var to = source.IndexOf(end, from + start.Length, StringComparison.Ordinal);
        to.Should().BeGreaterThan(from, $"the source should contain {end} after {start}");
        return source[from..to];
    }

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray())).Replace("\r\n", "\n");

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
