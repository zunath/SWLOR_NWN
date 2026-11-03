using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Value moving between the character file (saved on export) and the database (saved at once): shuttle tickets, ship
/// repairs, freight deposits, drill ore, space lockbox grants and design schematics. Each step is ordered and marked so a
/// restart after any line neither keeps both sides nor loses either, and every debit runs on the payer's queue.
/// </summary>
public class SpaceTransferCrashSafetyTests
{
    // Shuttle tickets

    [Test]
    public void ShuttleTickets_ArePaidOnThePlayersQueueAndSavedWithThePaymentsId()
    {
        var dialog = Source("Feature", "DialogDefinition", "StarportFlightsDialog.cs");
        dialog.Should().NotContain("TakeGoldFromCreature(");

        var shuttle = Source("Service", "Shuttle.cs");
        var purchase = Method(shuttle, "public static bool TryPurchaseTicket(uint player, ShuttleRoute route, string cityPropertyId, float taxRate)");
        purchase.Should().NotContain("TakeGoldFromCreature(").And.NotContain("DB.Set(", "the dialog handler only checks and queues");
        purchase.Should().Contain("AssignCommand(player, () => CompleteTicketPurchase(player, route, cityPropertyId, taxRate));");

        InOrder(Method(shuttle, "private static void CompleteTicketPurchase("),
            "if (!CanPurchaseTicket(player, route, taxRate, out var fare, out var tax))",
            "CreditPayments.NewMark(CreditPaymentKind.ShuttleTicket, ride.Id, fare + tax);",
            "CreditPayments.Take(player, mark);",
            "ride.PaymentId = mark.Id;",
            "DB.Set(ride);",
            "CreditPayments.Retire(player);",
            "dbCity.Treasury += tax;");
        InOrder(Method(shuttle, "private static bool CanPurchaseTicket("),
            "if (GetRide(GetObjectUUID(player)) != null)", "if (GetGold(player) < fare + tax)");

        var settle = Method(Source("Service", "CreditPayments.cs"), "private static string GetRecordPaymentId(");
        settle.Should().Contain("CreditPaymentKind.ShuttleTicket => DB.Get<ShuttleRide>(mark.RecordId)?.PaymentId");
        new ShuttleRide().PaymentId.Should().BeEmpty("older tickets name no payment");

        var ticket = new CreditPaymentMark { Id = "t1", Kind = CreditPaymentKind.ShuttleTicket, RecordId = "ride", Credits = 120 };
        ticket.KeepsPayment(null).Should().BeFalse("the ticket was never saved, so the price comes back");
        ticket.KeepsPayment("t1").Should().BeTrue();
        CreditPaymentMark.TryParse(ticket.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(ticket);
    }

    [Test]
    public void AShuttleRefund_IsSavedBeforeTheTicketIsDeleted()
    {
        var shuttle = Source("Service", "Shuttle.cs");
        InOrder(Method(shuttle, "public static bool RefundTicket("),
            "GiveGoldToCreature(player, ride.FarePaid);",
            "ObjectPlugin.SetString(player, RefundedRideVariable, ride.Id, true);",
            "ExportSingleCharacter(player);",
            "DB.Delete<ShuttleRide>(ride.Id);");
        shuttle.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void SettleRefundedTicket()");
        InOrder(Method(shuttle, "public static void SettleRefundedTicket("),
            "ride.PlayerId != playerId || ride.Status != ShuttleRideStatus.Ticketed", "DB.Delete<ShuttleRide>(ride.Id);");
    }

    // Ship repairs

    [Test]
    public void ShipRepairs_AreSavedWithThePaymentsIdOnThePlayersQueue()
    {
        var viewModel = Source("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs");
        var click = Method(viewModel, "public Action OnClickRepair()");
        click.Should().NotContain("TakeGoldFromCreature").And.NotContain("DB.Set(", "the modal never saves the ship it read earlier");
        click.Should().Contain("AssignCommand(player, () => CompleteRepair(player, shipId, price));");

        InOrder(Method(viewModel, "private void CompleteRepair("),
            "var dbShip = GetShipDockedHere(shipId);",
            "if (CalculateRepairBill(dbShip) != price)",
            "if (GetGold(player) < price)",
            "CreditPayments.NewMark(CreditPaymentKind.ShipRepair, dbShip.Id, price);",
            "CreditPayments.Take(player, mark);",
            "dbShip.Status.Hull = dbShip.Status.MaxHull;",
            "CreditPayments.RecordOn(dbShip, player, mark);",
            "DB.Set(dbShip);",
            "CreditPayments.Retire(player);");

        Method(Source("Service", "CreditPayments.cs"), "private static string GetRecordPaymentId(")
            .Should().Contain("CreditPaymentKind.HullRefit or CreditPaymentKind.CustomsFine or CreditPaymentKind.ShipRepair\n                    => DB.Get<PlayerShip>(mark.RecordId)?.PaymentIds?.GetValueOrDefault(GetObjectUUID(player)),");
        var repair = new CreditPaymentMark { Id = "r1", Kind = CreditPaymentKind.ShipRepair, RecordId = "ship", Credits = 800 };
        repair.KeepsPayment("an older refit").Should().BeFalse();
        repair.KeepsPayment("r1").Should().BeTrue();
    }

    // Freight deposits

    [Test]
    public void AFreightDeposit_IsCheckedAgainOnTheQueue_AndAnUnpaidContractIsCancelled()
    {
        var freight = Source("Service", "TradeFreight.cs");
        InOrder(Method(freight, "private static void PayFreightDeposit("),
            "if (contract == null || !contract.DepositPending)",
            "if (GetGold(player) < deposit)",
            "CancelUnpaidContract(dbPlayer, contract);",
            "return;",
            "TakeGoldFromCreature(deposit, player, true);",
            "ObjectPlugin.SetString(player, DepositPaidVariable, contractId, true);",
            "ExportSingleCharacter(player);",
            "contract.DepositPending = false;");
        InOrder(Method(freight, "private static void CancelUnpaidContract("),
            "ship.SealedFreight.RemoveAll(x => x.ContractId == contract.Id)", "DB.Set(ship);", "dbPlayer.FreightContracts.Remove(contract);");
        Method(freight, "public static void ReconcileFreightDeposits(").Should().Contain("CancelUnpaidContract(dbPlayer, contract);");
    }

    // Asteroid Drill ore

    [Test]
    public void ADrillCollection_RoundTripsAndLeavesAndReturnsToTheStock()
    {
        var collection = new DrillCollection { Id = "c1", BuildingId = "b1", Units = new Dictionary<string, int> { ["ore_a"] = 3, ["ore_b"] = 2 } };
        DrillCollection.TryParse(collection.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(collection);
        DrillCollection.TryParse(null, out _).Should().BeFalse();
        DrillCollection.TryParse("c1|b1", out _).Should().BeFalse();
        DrillCollection.TryParse("|b1|ore_a=1", out _).Should().BeFalse();
        DrillCollection.TryParse("c1|b1|ore_a=0", out _).Should().BeFalse();
        DrillCollection.TryParse("c1|b1|ore_a", out _).Should().BeFalse();

        var storage = new Dictionary<string, int> { ["ore_a"] = 5, ["ore_b"] = 2 };
        collection.TakeFrom(storage);
        storage.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore_a"] = 2 }, "a resref collected out is removed");
        collection.ReturnTo(storage);
        storage.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore_a"] = 5, ["ore_b"] = 2 });

        DrillCollection.IsMarked(collection.Serialize(), "c1").Should().BeTrue();
        DrillCollection.IsMarked(collection.Serialize(), "c2").Should().BeFalse("an older mark does not hold a newer collection");
        DrillCollection.IsMarked(string.Empty, "c1").Should().BeFalse();
        new StationBuildingState().PendingDrillCollections.Should().BeEmpty("older drills have no pending collection");
    }

    [TestCase(false, false, 5, Description = "Restart before the character file saved the ore: the ore goes back to the stock")]
    [TestCase(true, false, 2, Description = "Restart after the character file saved the ore: the stock stays without it")]
    [TestCase(true, true, 2, Description = "Restart after the drill cleared the collection: nothing to settle")]
    public void ADrillCollectionARestartInterrupted_LeavesTheOreInExactlyOnePlace(bool characterSaved, bool drillCleared, int stockAfterLogin)
    {
        var collection = new DrillCollection { Id = "c1", BuildingId = "b1", Units = new Dictionary<string, int> { ["ore_a"] = 3 } };
        var state = new StationBuildingState { DrillStorage = new Dictionary<string, int> { ["ore_a"] = 5 } };
        collection.TakeFrom(state.DrillStorage);
        state.PendingDrillCollections[collection.Id] = collection.Serialize();
        var mark = characterSaved ? collection.Serialize() : string.Empty;
        if (drillCleared)
            state.PendingDrillCollections.Remove(collection.Id);

        // Another collector reads the stock before the first collector logs in: the reserved ore is not there.
        state.DrillStorage.GetValueOrDefault("ore_a").Should().Be(2);

        // SettleDrillCollections, for the first collector's login.
        if (state.PendingDrillCollections.TryGetValue(collection.Id, out var pending) &&
            DrillCollection.TryParse(pending, out var settled) && !DrillCollection.IsMarked(mark, settled.Id))
        {
            settled.ReturnTo(state.DrillStorage);
        }
        state.PendingDrillCollections.Remove(collection.Id);

        state.DrillStorage.GetValueOrDefault("ore_a").Should().Be(stockAfterLogin);
        (state.DrillStorage.GetValueOrDefault("ore_a") + (characterSaved ? 3 : 0)).Should().Be(5, "the ore is never in both places or neither");
    }

    [Test]
    public void DrillOre_LeavesTheStockBeforeTheCharacterFileSavesIt_AndALoginSettlesThePendingCollection()
    {
        var service = Source("Service", "StationBuildings.cs");
        var collect = Method(service, "public static void CollectDrill(");
        collect.Should().NotContain("state.DrillStorage.Remove(").And.NotContain("state.DrillStorage[resref] =",
            "the stock changes only through the collection");
        InOrder(collect,
            "var item = CreateItemOnObject(resref, player);",
            "DB.Set(new DrillCollectionReservation { Id = collection.Id, OwnerPlayerId = GetObjectUUID(player), BuildingId = buildingId });",
            "collection.TakeFrom(state.DrillStorage);",
            "state.PendingDrillCollections[collection.Id] = collection.Serialize();",
            "DB.Set(state);",
            "ObjectPlugin.SetString(player, DrillCollectionVariable, collection.Serialize(), true);",
            "ExportSingleCharacter(player);",
            "state.PendingDrillCollections.Remove(collection.Id);",
            "DB.Set(state);",
            "DB.Delete<DrillCollectionReservation>(collection.Id);");
        service.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void SettleDrillCollections()");
        InOrder(Method(service, "public static void SettleDrillCollections("),
            ".AddFieldSearch(nameof(DrillCollectionReservation.OwnerPlayerId), GetObjectUUID(player), false);",
            "var mark = ObjectPlugin.GetString(player, DrillCollectionVariable);",
            "state.PendingDrillCollections.TryGetValue(reservation.Id, out var pending)",
            "!DrillCollection.IsMarked(mark, collection.Id)",
            "collection.ReturnTo(state.DrillStorage);",
            "state.PendingDrillCollections.Remove(reservation.Id);",
            "DB.Set(state);",
            "DB.Delete<DrillCollectionReservation>(reservation.Id);");
        typeof(DrillCollectionReservation).GetProperty(nameof(DrillCollectionReservation.OwnerPlayerId))!
            .GetCustomAttributes(typeof(IndexedAttribute), false).Should().NotBeEmpty();
    }

    // Space lockboxes

    [Test]
    public void ALockboxGrant_RoundTripsThroughTheCharacterFileLocal()
    {
        var grant = new SpaceLockboxGrant
        {
            Id = "g1",
            Intel = 2,
            Unplaced = new List<(string Resref, ShipComponentTraitType? Trait)>
            {
                ("ship_missile_ion", null),
                ("sc_emitter_1", ShipComponentTraitType.Overcharged),
            },
        };
        SpaceLockboxGrant.TryParse(grant.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(grant);
        parsed.IsEmpty.Should().BeFalse();

        SpaceLockboxGrant.TryParse("g2|0|", out var empty).Should().BeTrue();
        empty.IsEmpty.Should().BeTrue("a lockbox whose rewards all fit and hold no intel keeps nothing in the database");
        SpaceLockboxGrant.TryParse(null, out _).Should().BeFalse();
        SpaceLockboxGrant.TryParse("g1|-1|", out _).Should().BeFalse();
        SpaceLockboxGrant.TryParse("g1|1", out _).Should().BeFalse();
        SpaceLockboxGrant.TryParse("g1|1|res~x", out _).Should().BeFalse();
        new Player().LastLockboxGrantId.Should().BeEmpty();
    }

    [Test]
    public void ALockboxsDatabaseRewards_WaitForASaveWithoutTheLockbox()
    {
        var slicing = Source("Service", "SpaceSlicing.cs");
        var give = Method(slicing, "private static void GiveRewardsToPlayer(");
        InOrder(give,
            "case SpaceSlicingRewardKind.AgentIntel:\n                        intel++;",
            "if (fromLockbox)",
            "KeepLockboxGrantAfterSave(player, new SpaceLockboxGrant { Id = Guid.NewGuid().ToString(), Intel = intel, Unplaced = unplaced });",
            "return;",
            "GrantAgentIntel(player);",
            "ShipHold.KeepUnplacedYield(player, unplaced);");

        InOrder(Method(slicing, "private static void KeepLockboxGrantAfterSave("),
            "ObjectPlugin.SetString(player, LockboxGrantVariable, grant.Serialize(), true);",
            "AssignCommand(GetModule(), () => DelayCommand(0.1f, () =>",
            "if (!GetIsObjectValid(player))",
            "ExportSingleCharacter(player);",
            "KeepLockboxGrant(player, grant);");
        InOrder(Method(slicing, "private static void KeepLockboxGrant("),
            "FactionAgents.GrantLockboxIntel(player, grant.Intel, grant.Id);",
            "ShipHold.KeepUnplacedYield(player, grant.Unplaced, grant.Id);",
            "ObjectPlugin.DeleteString(player, LockboxGrantVariable);",
            "ExportSingleCharacter(player);");
        slicing.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void SettleLockboxGrant()");

        // Keeping a grant again changes nothing: its intel is saved with its id, and its hold entries are named from it.
        InOrder(Method(Source("Service", "FactionAgents.cs"), "public static void GrantLockboxIntel("),
            "dbPlayer.LastLockboxGrantId == grantId", "dbPlayer.AgentIntel += count;", "dbPlayer.LastLockboxGrantId = grantId;", "DB.Set(dbPlayer);");
        var hold = Source("Service", "ShipHold.cs");
        Method(hold, "public static void KeepUnplacedYield(").Should()
            .Contain("string NextKey() => entryPrefix == null ? null : $\"{entryPrefix}:{entry++}\";")
            .And.Contain("StoreComponent(hold, (resref, trait.Value), NextKey());");
        Method(hold, "private static int AddNewStack(").Should().Contain("hold.Items[key ?? Guid.NewGuid().ToString()]");
        Method(hold, "private static string StoreComponent(").Should().Contain("hold.Items[key ?? Guid.NewGuid().ToString()]");
    }

    // Design schematics

    [Test]
    public void ADesignSchematic_IsSpentAndSavedBeforeTheDesignIsSavedAsLearned()
    {
        var item = Source("Feature", "ItemDefinition", "DesignSchematicItemDefinition.cs");
        item.Should().Contain(".ApplyAction((user, item, _, _, _) => Space.LearnHullDesign(user, item));")
            .And.NotContain("DB.Set(", "the design is saved only after a save without the schematic");

        var designs = Source("Service", "SpaceHullDesigns.cs");
        InOrder(Method(designs, "public static void LearnHullDesign("),
            "dbPlayer.UnlockedHullDesigns.ContainsKey(HullDesignRules.Key(design))",
            "Item.ReduceItemStack(schematic, 1);",
            "ObjectPlugin.SetString(user, LearnedDesignVariable, resref, true);",
            "AssignCommand(GetModule(), () => DelayCommand(0.1f, () =>",
            "if (!GetIsObjectValid(user))",
            "ExportSingleCharacter(user);",
            "SaveLearnedDesign(user, resref);");
        Method(designs, "public static void LearnHullDesign(").Should().NotContain("DB.Set(");
        InOrder(Method(designs, "private static void SaveLearnedDesign("),
            "HullDesignRules.Unlock(dbPlayer.UnlockedHullDesigns, design, DateTime.UtcNow)", "DB.Set(dbPlayer);",
            "if (ObjectPlugin.GetString(player, LearnedDesignVariable) == resref)");
        designs.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void SettleLearnedHullDesign()");
    }

    // Ship modules and deeds

    [Test]
    public void ModulesAndDeeds_AreMarkedAndSavedWithTheCharacterBeforeTheShipChanges()
    {
        var viewModel = Source("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs");
        var power = Method(viewModel, "private void ProcessPower(");
        InOrder(power, "SerializedItem = ObjectPlugin.Serialize(item),", "Space.MarkModuleFitting(Player, item, shipId);", "DB.Set(dbShip);",
            "Space.SaveModuleReceipt(Player, GetObjectUUID(item), shipId);", "DestroyObject(item);");
        InOrder(power, "if (!Item.TryAcquireItem(Player, item))", "Space.BeginModuleRemoval(Player, item, shipId, fitted);",
            "currentModules.Remove(slot);", "DB.Set(current);", "Space.FinishModuleRemoval(Player, item, fitted.ItemInstanceId);");
        var config = Method(viewModel, "private void ProcessConfiguration(");
        InOrder(config, "SerializedItem = ObjectPlugin.Serialize(item),", "Space.MarkModuleFitting(Player, item, shipId);", "DB.Set(dbShip);",
            "Space.SaveModuleReceipt(Player, GetObjectUUID(item), shipId);", "DestroyObject(item);");
        InOrder(config, "if (!Item.TryAcquireItem(Player, item))", "Space.BeginModuleRemoval(Player, item, shipId, fitted);",
            "current.Status.ConfigurationModules.Remove(slot);", "DB.Set(current);", "Space.FinishModuleRemoval(Player, item, fitted.ItemInstanceId);");

        InOrder(Method(viewModel, "public Action OnClickRegisterShip()"),
            "SerializedItem = ObjectPlugin.Serialize(item),", "Space.MarkDeedRegistering(Player, item, ship.Id);", "DB.Set(ship);", "DestroyObject(item);");
        var unregister = Method(viewModel, "public Action OnClickUnregisterShip()");
        InOrder(unregister, "if (!Item.TryAcquireItem(Player, deed))", "Space.MarkDeedUnregistered(Player, deed, shipId);",
            "Space.RemoveUnregisteredShip(shipId);", "Space.ClearDeedUnregistered(deed);");
        unregister.Should().Contain("dbShip.Status.ConfigurationModules.Count > 0", "fitted configuration modules would be deleted with the ship");
        unregister.Should().NotContain("DB.Delete<PlayerShip>(", "the ship goes only through RemoveUnregisteredShip");

        var moves = Source("Service", "SpaceShipItemMoves.cs");
        InOrder(Method(moves, "public static void MarkModuleFitting("), "SetLocalString(item, ModuleFittingVariable,", "ExportSingleCharacter(player);");
        var beginRemoval = Method(moves, "public static void BeginModuleRemoval(");
        InOrder(beginRemoval, "DB.Set(new ShipModuleRemoval", "SetLocalString(item, ModuleRemovedVariable,");
        beginRemoval.Should().NotContain("ExportSingleCharacter", "the character is saved only after the ship drops the module");
        InOrder(Method(moves, "public static void FinishModuleRemoval("), "ExportSingleCharacter(player);", "DB.Delete<ShipModuleRemoval>(instanceId);");
        InOrder(Method(moves, "public static void MarkDeedRegistering("), "SetLocalString(deed, DeedRegisteringVariable, shipId);", "ExportSingleCharacter(player);");
        InOrder(Method(moves, "public static void MarkDeedUnregistered("), "SetLocalString(deed, DeedUnregisteredVariable, shipId);", "ExportSingleCharacter(player);");
        InOrder(Method(moves, "public static void RemoveUnregisteredShip("),
            "dbPlayer.ActiveShipId = Guid.Empty.ToString();", "UndockAllToStarport(dbShip, \"was unregistered\");",
            "ReturnShipDroidCrew(dbShip, \"was unregistered\");", "ShipTransfer.RemoveListings(shipId);",
            "dbProperty.IsQueuedForDeletion = true;", "DB.Delete<PlayerShip>(shipId);");

        moves.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void SettleShipItemMoves()");
        var settle = Method(moves, "public static void SettleShipItemMoves(");
        InOrder(settle, "TryReadShipMark(item, ModuleFittingVariable, out var shipId, out var instanceId)",
            "if (DB.Get<ShipModuleReceipt>(instanceId) != null || FindFittedModule(DB.Get<PlayerShip>(shipId), instanceId) != null)",
            "DestroyObject(item);");
        InOrder(settle, "DelayCommand(1f, () =>", "ExportSingleCharacter(player);", "DB.Delete<ShipModuleReceipt>(receiptId);");
        new ShipModuleReceipt().OwnerPlayerId.Should().BeEmpty();
        InOrder(settle, "TryReadShipMark(item, ModuleRemovedVariable, out _, out instanceId)", "removedHeld.Add(instanceId);",
            "SettleModuleRemovals(player, playerId, removedHeld)", "DB.Delete<ShipModuleRemoval>(removalId);");
        settle.Should().NotContain("fitted.Value.Modules.Remove(", "a login never takes a module off a ship another refitter may already hold");
        InOrder(Method(moves, "private static List<string> SettleModuleRemovals("),
            "ShipModuleRemovalRules.Settle(removedHeld.Contains(removal.Id), stillFitted)",
            "case ShipModuleRemovalOutcome.NeverHappened:", "DB.Delete<ShipModuleRemoval>(removal.Id);",
            "case ShipModuleRemovalOutcome.GiveAgain:", "Item.TryAcquireItem(player, item)",
            "SetLocalString(item, ModuleRemovedVariable,", "givenAgain.Add(removal.Id);");
        new ShipModuleRemoval().OwnerPlayerId.Should().BeEmpty();
        typeof(ShipModuleRemoval).GetProperty(nameof(ShipModuleRemoval.OwnerPlayerId))!
            .GetCustomAttributes(typeof(IndexedAttribute), false).Should().NotBeEmpty();
        InOrder(settle, "GetLocalString(item, DeedRegisteringVariable)", "if (DB.Get<PlayerShip>(registering) != null)", "DestroyObject(item);");
        InOrder(settle, "GetLocalString(item, DeedUnregisteredVariable)", "ship.OwnerPlayerId == playerId",
            "RemoveUnregisteredShip(unregistered);");
    }

    [TestCase(true, true, ShipModuleRemovalOutcome.Finished)]
    [TestCase(true, false, ShipModuleRemovalOutcome.Finished)]
    [TestCase(false, true, ShipModuleRemovalOutcome.NeverHappened)]
    [TestCase(false, false, ShipModuleRemovalOutcome.GiveAgain)]
    public void AModuleRemovalARestartInterrupted_LeavesExactlyOneCopy(bool characterHolds, bool shipStillHas, ShipModuleRemovalOutcome expected)
    {
        // The record is saved before the ship drops the module and the character after it, so: the character holding it
        // means done; the ship still having it means it never left (another refitter may now take it, replacing the
        // record); neither means the ship dropped it before the character was saved, so it is given again.
        ShipModuleRemovalRules.Settle(characterHolds, shipStillHas).Should().Be(expected);
    }

    private static void InOrder(string source, params string[] parts)
    {
        var at = -1;
        foreach (var part in parts)
        {
            var next = source.IndexOf(part, at + 1, StringComparison.Ordinal);
            next.Should().BeGreaterThan(at, $"'{part}' comes next");
            at = next;
        }
    }

    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);
        var depth = 0;
        for (var i = source.IndexOf('{', start); i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0)
                return source.Substring(start, i - start + 1);
        }

        return source[start..];
    }

    private static string Source(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
