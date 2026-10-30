#nullable enable
using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Tests.Feature.SpaceItemFixtures;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The W11n review fixes: retried space item refunds, cursed items nested in bags, incursion credit for pilots who log
/// out, and pending incursion spoils that survive a disconnect before delivery.
/// </summary>
public class SpaceW11nReviewFixTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server"), Path.Combine));

    // Refunds survive a retry

    [Test]
    public void Refund_IsPaidOncePerSourceKey_WhateverTheRetryOrder()
    {
        var player = new Player();
        LegacySpaceItemSweep.Pay(player, 750, Array.Empty<string>(), "InventoryItem:a#root").Should().BeTrue();
        player.MarketTill.Should().Be(750);
        player.RetiredSpaceRefunds.Should().Equal("InventoryItem:a#root");

        LegacySpaceItemSweep.Pay(player, 750, Array.Empty<string>(), "InventoryItem:a#root").Should().BeFalse("a rerun finds the key already paid");
        player.MarketTill.Should().Be(750);
        player.UpgradeNotices.Should().ContainSingle();

        LegacySpaceItemSweep.Pay(player, 200, Array.Empty<string>(), "InventoryItem:b#nested").Should().BeTrue();
        player.MarketTill.Should().Be(950, "a different stored item is a different refund");
    }

    [Test]
    public void ShipRefund_KeyedByTheShip_IsNotPaidTwiceWhenTheShipSaveFailedFirstTime()
    {
        var player = new Player();
        // First attempt: the owner is saved (credit and key together), then the run stops before the ship is saved.
        LegacySpaceItemSweep.Pay(player, 1500, new[] { "sd_republicstrik:10073" }, "PlayerShip:s1").Should().BeTrue();
        // Retry: the ship is still retired, so it converts and pays again.
        LegacySpaceItemSweep.Pay(player, 1500, new[] { "sd_republicstrik:10073" }, "PlayerShip:s1").Should().BeFalse();
        player.MarketTill.Should().Be(1500);

        var zero = new Player();
        LegacySpaceItemSweep.Pay(zero, 0, Array.Empty<string>(), "PlayerShip:s2").Should().BeTrue("the ship is recorded even when it earns nothing");
        zero.RetiredSpaceRefunds.Should().Contain("PlayerShip:s2");
    }

    [Test]
    public void TheSweep_PaysAndSavesOwnersBeforeConvertedRecordsAreSaved()
    {
        var pass = ReadSource("Feature", "MigrationDefinition", "StoredItemPasses.cs").Replace("\r\n", "\n");
        pass.IndexOf("beforeSave?.Invoke();").Should().BeGreaterThan(pass.IndexOf("Parallel.For"))
            .And.BeLessThan(pass.IndexOf("DB.Set(batch[index]);"), "refunds are written before the batch of records that held the items");

        var sweep = ReadSource("Feature", "MigrationDefinition", "StoredItemSweep.cs").Replace("\r\n", "\n");
        foreach (var record in new[] { "InventoryItem", "MarketItem", "WorldPropertyCategory" })
            sweep.Should().Contain($"nameof({record})", $"{record} refunds are keyed by their record");
        sweep.Should().Contain("space == null ? null : space.Flush");

        var apply = ReadSource("Feature", "MigrationDefinition", "LegacySpaceItemSweep.cs").Replace("\r\n", "\n");
        apply.IndexOf("Flush();").Should().BeLessThan(apply.IndexOf("DB.Delete<InventoryItem>(id);"), "the owner is paid before the module record is deleted");

        ReadSource("Feature", "MigrationDefinition", "ServerMigration", "_30_ConvertSpaceItems.cs")
            .Should().Contain("LegacySpaceItemSweep.Pay(owner, result.Items.Credits, result.Items.HullDesigns, shipKey)");
    }

    [Test]
    public void ConfigurationModuleInPropertyStorage_IsRefundedToThePropertyOwnerByKey()
    {
        var sweep = ReadSource("Feature", "MigrationDefinition", "StoredItemSweep.cs").Replace("\r\n", "\n");
        sweep.Should().Contain("space.QueueRefund(owner, sourceKey, credits);", "the removed module pays its owner");
        sweep.Should().Contain("var owner = LegacySpaceItemSweep.ForProperty(category.ParentPropertyId);");
        sweep.Should().Contain("$\"{nameof(WorldPropertyCategory)}:{category.Id}:{key}\"");

        LoadBlueprints();
        var removedWith = -1;
        var sweepRule = new LegacySpaceItemSweep();
        sweepRule.Migrate(Item("config_fig3"), _ => { }, "property:p1", "WorldPropertyCategory:c1:k1", rootConfiguration: credits => removedWith = credits);
        removedWith.Should().Be(750);
        sweepRule.QueueRefund("property:p1", "WorldPropertyCategory:c1:k1", removedWith);

        var owner = new Player();
        LegacySpaceItemSweep.Pay(owner, removedWith, Array.Empty<string>(), "WorldPropertyCategory:c1:k1#root").Should().BeTrue();
        LegacySpaceItemSweep.Pay(owner, removedWith, Array.Empty<string>(), "WorldPropertyCategory:c1:k1#root").Should().BeFalse("a rerun does not pay twice");
        owner.MarketTill.Should().Be(750);
    }

    // Cursed items nested in containers

    private static bool ContainsCursed(string data) =>
        (bool)Document.GetMethod("ContainsCursedItem", Members, null, new[] { typeof(string) }, null)!.Invoke(null, new object[] { data })!;

    private static object Cursed(object node)
    {
        var fields = (IList)NodeType.GetField("Fields")!.GetValue(node)!;
        var cursed = fields.Cast<object>().First(x => (string)x.GetType().GetProperty("Name")!.GetValue(x)! == "Cursed");
        cursed.GetType().GetField("Data")!.SetValue(cursed, BitConverter.GetBytes(1));
        return node;
    }

    private static object InBag(string bag, params object[] contents) =>
        ItemNode(bag, new[] { List("ItemList", contents) });

    [Test]
    public void CursedCheck_SeesAFlagOnTheItemItself_AndNestedAtAnyDepth()
    {
        ContainsCursed(Item("bag_sm")).Should().BeFalse();
        ContainsCursed(Encode(Cursed(ItemNode("bag_sm")), "UTI ")).Should().BeTrue("the item itself");

        var plain = ItemNode("min_laser_4", new[] { Scalar(2, "Repos_PosX", 0), Scalar(2, "Repos_Posy", 0) }, type: 0);
        ContainsCursed(Encode(InBag("bag_sm", plain), "UTI ")).Should().BeFalse();

        var bound = Cursed(ItemNode("min_laser_4", new[] { Scalar(2, "Repos_PosX", 0), Scalar(2, "Repos_Posy", 0) }, type: 0));
        ContainsCursed(Encode(InBag("bag_sm", bound), "UTI ")).Should().BeTrue("a bound item inside a bag");

        var deep = InBag("bag_sm", ItemNode("bag_sm", new[] { Scalar(2, "Repos_PosX", 0), Scalar(2, "Repos_Posy", 0) }, type: 0));
        ContainsCursed(Encode(deep, "UTI ")).Should().BeFalse();
        var nested = Cursed(ItemNode("min_laser_4", new[] { Scalar(2, "Repos_PosX", 0), Scalar(2, "Repos_Posy", 0) }, type: 0));
        var innerBag = InBag("bag_sm", nested);
        ((IList)NodeType.GetField("Fields")!.GetValue(innerBag)!).Count.Should().BeGreaterThan(0);
        ContainsCursed(Encode(InBag("bag_sm", innerBag), "UTI ")).Should().BeTrue("a bound item two bags deep");
    }

    [Test]
    public void ShipTransferChecks_UseTheNestedCheck_ForModulesAndStoredItems()
    {
        var source = ReadSource("Service", "ShipTransfer.cs");
        source.Should().Contain("StoredObjectData.ContainsCursedItem(data)");
        source.Should().NotContain("GetItemCursedFlag(item)", "the old check only read the flag of the top item");
    }

    // Incursion credit by player id

    private static ActiveIncursion Incursion() => new();

    [Test]
    public void KillCredit_GoesToAPilotWhoLoggedOut_ById()
    {
        var incursion = Incursion();
        var damage = new Dictionary<string, double> { ["offline-pilot"] = 300, ["online-pilot"] = 100 };
        IncursionRules.CreditKill(incursion, 2, damage, 4.0, "offline-pilot", pilot => pilot == "online-pilot"
            ? new List<(string, string)> { ("online-pilot", "Ace"), ("gunner", "Gunner") }
            : new List<(string, string)> { ("offline-pilot", "Gone") });

        var offline = incursion.Contributions["offline-pilot"];
        offline.Name.Should().Be("Gone");
        offline.Kills.Should().Be(1, "the killing blow belongs to the pilot who logged out");
        offline.Damage.Should().Be(300);
        offline.TotalPoints().Should().BeGreaterThan(3.0, "three quarters of the points plus the kill bonus");

        incursion.Contributions["online-pilot"].TotalPoints().Should().BeApproximately(incursion.Contributions["gunner"].TotalPoints(), 1e-9);
        incursion.Contributions["online-pilot"].Kills.Should().Be(0);
        incursion.Contributions["gunner"].Damage.Should().Be(50);
    }

    [Test]
    public void KillCredit_SkipsAPilotWithNoOneToCredit()
    {
        var incursion = Incursion();
        IncursionRules.CreditKill(incursion, 1, new Dictionary<string, double> { ["x"] = 10 }, 1.0, "x", _ => new List<(string, string)>());
        incursion.Contributions.Should().BeEmpty();
    }

    [Test]
    public void IncursionDamage_IsRecordedByPersistentId_NotCreatureHandle()
    {
        var source = ReadSource("Service", "SpaceIncursions.cs");
        source.Should().Contain("Dictionary<uint, Dictionary<string, double>> _incursionDamage");
        source.Should().Contain("var pilotId = GetObjectUUID(ship);");
        source.Should().Contain("IncursionRules.CreditKill(incursion, tier, damage, points, killer");
    }

    // Pending spoils stay pending until delivery

    [Test]
    public void PendingSpoils_AreOnlyTakenWhenThePlayerCanReceiveThem()
    {
        var pending = new Dictionary<string, IncursionPayout> { ["p1"] = new() { Credits = 500 } };

        IncursionRules.ClaimPending(pending, "p1", false).Should().BeNull("the player disconnected before the delay ended");
        pending.Should().ContainKey("p1");

        IncursionRules.ClaimPending(pending, "p1", true)!.Credits.Should().Be(500);
        pending.Should().BeEmpty();
        IncursionRules.ClaimPending(pending, "p1", true).Should().BeNull("a second callback finds nothing to deliver twice");
    }

    [Test]
    public void AClearedIncursion_SavesEverySpoilAsPendingBeforeAnyoneCollects()
    {
        var source = ReadSource("Service", "SpaceIncursions.cs").Replace("\r\n", "\n");
        var start = source.IndexOf("private static void CompleteIncursion(", StringComparison.Ordinal);
        var complete = source[start..source.IndexOf("private static void EndIncursion(", start, StringComparison.Ordinal)];
        complete.Should().Contain("CapitalBattleRules.AddPending(state.PendingPayouts, playerId, RollPulls(payout));");
        complete.Should().NotContain("DeliverPayout(");
        complete.IndexOf("EndIncursion(true);", StringComparison.Ordinal)
            .Should().BeLessThan(complete.IndexOf("CollectPendingIncursionPayout(player, playerId);", StringComparison.Ordinal),
                "the incursion's end and its owed spoils are saved before anyone collects");
    }

    [Test]
    public void AClearedRiftRun_SavesEverySpoilAsPendingBeforeAnyoneCollects()
    {
        var source = ReadSource("Service", "SpaceRiftRuns.cs").Replace("\r\n", "\n");
        var start = source.IndexOf("private static void PayRiftRun(", StringComparison.Ordinal);
        var pay = source[start..source.IndexOf("public static void DeliverPendingRiftRunPayout()", start, StringComparison.Ordinal)];
        pay.Should().Contain("CapitalBattleRules.AddPending(state.PendingRiftPayouts, pilotId,");
        pay.Should().Contain("CapitalBattleRules.AddPending(state.PendingRiftPayouts, playerId,");
        pay.Should().NotContain("GiveGoldToCreature(");
        pay.Should().NotContain("ShipHold.DeliverYield(");
        pay.IndexOf("SaveIncursions();", StringComparison.Ordinal)
            .Should().BeLessThan(pay.IndexOf("CollectPendingPayout(player, state.PendingRiftPayouts,", StringComparison.Ordinal),
                "every share of the clear is saved before anyone collects");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void DeliverPendingRiftRunPayout()");
    }

    [Test]
    public void PendingSpoils_AreClearedOnlyAfterTheyAreDelivered()
    {
        var source = ReadSource("Service", "SpaceIncursions.cs").Replace("\r\n", "\n");
        var collect = source[source.IndexOf("private static void CollectPendingPayout(", StringComparison.Ordinal)..];
        collect.IndexOf("DeliverPayout(player, payout, heading, logLabel);", StringComparison.Ordinal)
            .Should().BeLessThan(collect.IndexOf("IncursionRules.ClaimPending(pending, playerId, true);", StringComparison.Ordinal));
        collect.IndexOf("IncursionRules.ClaimPending(pending, playerId, true);", StringComparison.Ordinal)
            .Should().BeLessThan(collect.IndexOf("SaveIncursions();", StringComparison.Ordinal));
        ReadSource("Service", "SpaceCapitalBattles.cs").Should().Contain("CollectPendingPayout(player, IncursionRecord.PendingBattlePayouts, playerId,");
    }

    [Test]
    public void UnregisteringAShip_IsRefusedWhileAnyModuleIsFitted()
    {
        ReadSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs").Replace("\r\n", "\n")
            .Should().Contain("dbShip.Status.HighPowerModules.Count > 0 ||\n                        dbShip.Status.MidPowerModules.Count > 0 ||\n                        dbShip.Status.LowPowerModules.Count > 0");
    }

    [Test]
    public void PendingSpoils_AreRolledOnce_AndSavedWithTheCharacterBeforeTheyAreCleared()
    {
        var incursions = ReadSource("Service", "SpaceIncursions.cs").Replace("\r\n", "\n");
        incursions.Should().Contain("CapitalBattleRules.AddPending(state.PendingPayouts, playerId, RollPulls(payout));");
        ReadSource("Service", "SpaceCapitalBattles.cs").Should().Contain("CapitalBattleRules.AddPending(state.PendingBattlePayouts, playerId, RollPulls(payout));");

        var deliver = Between(incursions, "private static void DeliverPayout(", "/// <summary>A player who was offline");
        deliver.IndexOf("ShipHold.DeliverYield(", StringComparison.Ordinal)
            .Should().BeLessThan(deliver.IndexOf("ExportSingleCharacter(player);", StringComparison.Ordinal));
    }

    [Test]
    public void FreightProceeds_ArePaidAndSavedBeforeTheContractIsCleared()
    {
        var deliver = Between(ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n"),
            "public static void DeliverFreight(", "public static void AbandonFreight(");
        var paid = deliver.IndexOf("GiveGoldToCreature(player, result.Fee + result.DepositReturned);", StringComparison.Ordinal);
        paid.Should().BeGreaterThan(0);
        paid.Should().BeLessThan(Index(deliver, "ObjectPlugin.SetString(player, PaidContractVariable, contract.Id, true);"));
        Index(deliver, "ObjectPlugin.SetString(player, PaidContractVariable, contract.Id, true);")
            .Should().BeLessThan(Index(deliver, "ExportSingleCharacter(player);"), "the mark is saved in the same character save as the proceeds");
        Index(deliver, "ExportSingleCharacter(player);").Should().BeLessThan(Index(deliver, "ClearContract(ship, dbPlayer, contract);"));
    }

    [Test]
    public void APaidFreightContract_IsClearedAtLoginWithoutPayingAgain()
    {
        var source = ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n");
        var login = Between(source, "public static void ClearPaidContract()", "private static void ClearContract(");
        login.Should().Contain("ObjectPlugin.GetString(player, PaidContractVariable)");
        login.Should().Contain("ClearContract(DB.Get<PlayerShip>(contract.ShipId), dbPlayer, contract);").And.NotContain("GiveGoldToCreature");
        Index(source, "[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void ClearPaidContract()").Should().BeGreaterThan(0);

        var clear = Between(source, "private static void ClearContract(", "public static void DeliverFreight(");
        Index(clear, "DB.Set(ship);").Should().BeLessThan(Index(clear, "DB.Set(dbPlayer);"),
            "the load goes before its contract, so a restart never leaves a load no contract can clear");
    }

    [Test]
    public void AgentMissionPay_SavesEachPartWithItsMark()
    {
        var source = ReadSource("Service", "FactionAgents.cs").Replace("\r\n", "\n");
        var pay = Between(source, "public static void PayMission(", "private static void FailMission(");
        pay.Should().Contain("Faction.AdjustPlayerFactionStanding(player, mission.Faction, mission.Standing, MarkHeld(missionId, x => x.PaidStanding = true));");
        pay.Should().Contain("Currency.GiveCurrency(player, currency, mission.Tokens, MarkHeld(missionId, x => x.PaidTokens = true));");
        Index(pay, "if (ObjectPlugin.GetString(player, PaidCreditsVariable) != missionId)")
            .Should().BeLessThan(Index(pay, "GiveGoldToCreature(player, mission.Credits);"), "credits already in the character file are not paid again");
        Index(pay, "ObjectPlugin.SetString(player, PaidCreditsVariable, missionId, true);")
            .Should().BeLessThan(Index(pay, "ExportSingleCharacter(player);"), "the mark is saved in the same character save as the credits");

        var faction = ReadSource("Service", "Faction.cs").Replace("\r\n", "\n");
        faction = Between(faction, "public static void AdjustPlayerFactionStanding(", "public static void AdjustPlayerFactionPoints(");
        Index(faction, "beforeSave?.Invoke(dbPlayer);").Should().BeLessThan(Index(faction, "DB.Set(dbPlayer);"));
        var currency = ReadSource("Service", "Currency.cs").Replace("\r\n", "\n");
        currency = currency[Index(currency, "public static void GiveCurrency(")..];
        Index(currency, "beforeSave?.Invoke(dbPlayer);").Should().BeLessThan(Index(currency, "DB.Set(dbPlayer);"));
    }

    [Test]
    public void AStationJump_SavesItsDestinationWithTheFuelAndFinishesAtBoot()
    {
        var source = ReadSource("Service", "StationBuildings.cs").Replace("\r\n", "\n");
        var jump = Between(source, "public static bool Jump(", "public static void FinishInterruptedJumps()");
        Index(jump, "record.PendingJumpDock = new PropertyLocation").Should().BeLessThan(Index(jump, "DB.Set(record);"));
        Index(jump, "CargoHoldRules.RemoveUnits(record.Cargo, CommodityType.StationFuel, fuel);").Should().BeLessThan(Index(jump, "DB.Set(record);"));
        Index(jump, "DB.Set(record);").Should().BeLessThan(Index(jump, "FinishJump(ship, record);"));

        Index(source, "[NWNEventHandler(ScriptName.OnModuleLoad)]\n        public static void FinishInterruptedJumps()").Should().BeGreaterThan(0);
        var finish = Between(source, "private static void FinishJump(", "private static void MoveDock(");
        Index(finish, "MoveDock(ship, record.PendingJumpDock, record.PendingJumpSpace);").Should().BeLessThan(Index(finish, "record.PendingJumpDock = null;"));
        Index(finish, "record.PendingJumpDock = null;").Should().BeLessThan(Index(finish, "DB.Set(record);"));
    }

    [Test]
    public void DrillOre_IsSavedWithTheCharacterBeforeTheStockIs()
    {
        var collect = Between(ReadSource("Service", "StationBuildings.cs").Replace("\r\n", "\n"),
            "public static void CollectDrill(", "// Droids");
        collect.IndexOf("ExportSingleCharacter(player);", StringComparison.Ordinal).Should().BeGreaterThan(0);
        collect.IndexOf("ExportSingleCharacter(player);", StringComparison.Ordinal)
            .Should().BeLessThan(collect.IndexOf("DB.Set(state);", StringComparison.Ordinal));
    }

    [Test]
    public void AShipTransfer_IsSavedBeforeTheShipChangesHands_AndARestartFinishesIt()
    {
        var source = ReadSource("Service", "ShipTransfer.cs").Replace("\r\n", "\n");
        var buy = Between(source, "public static bool Buy(", "private static void PaySaleProceeds(");
        Index(buy, "if (!IsStillListed(listing))")
            .Should().BeLessThan(Index(buy, "TakeGoldFromCreature("),
                "a listing the seller took off the market, or that expired, is refused before the buyer pays");
        Index(buy, "ship.SaleProceeds = (int)(listing.Price - listing.Price * taxRate);")
            .Should().BeLessThan(Index(buy, "Complete(ship, buyerId, \"was sold\");"),
                "the proceeds owed are saved with the transfer");

        var complete = Between(source, "public static void Complete(", "private static void ResetPermissions(");
        Index(complete, "ship.TransferFromPlayerId = ship.OwnerPlayerId;")
            .Should().BeLessThan(Index(complete, "DB.Set(ship);"));
        Index(complete, "DB.Set(ship);")
            .Should().BeLessThan(Index(complete, "RemoveListings(ship.Id);"),
                "the transfer, a gift or a sale, is saved before any listing or property changes");
        Index(complete, "var oldOwnerPlayerId = ship.TransferFromPlayerId;").Should().BeGreaterThan(0,
            "a finished-after-restart transfer clears the active ship of the owner it is leaving, not the new one");
        Index(complete, "dbOldOwner.ActiveShipId = Guid.Empty.ToString();")
            .Should().BeLessThan(Index(complete, "ship.TransferToPlayerId = string.Empty;"),
                "the transfer is cleared only after every step is saved");

        var recover = source[Index(source, "public static void FinishInterruptedTransfers()")..];
        Index(recover, "Complete(ship, ship.TransferToPlayerId, ship.TransferReason);")
            .Should().BeLessThan(Index(recover, "PaySaleProceeds(ship);"),
                "an interrupted transfer is finished before the seller is paid");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleLoad)]\n        public static void FinishInterruptedTransfers()");
        new PlayerShip().TransferToPlayerId.Should().BeEmpty("older ship records have no transfer under way");
    }

    [Test]
    public void AShipListing_ExpiresWhileTheServerStaysUp()
    {
        var source = ReadSource("Service", "ShipTransfer.cs").Replace("\r\n", "\n");
        source.Should().Contain("return IsStillListed(GetListing(shipId));");
        var still = Between(source, "private static bool IsStillListed(", "/// <summary>Whether the property is a ship listed for sale");
        Index(still, "< ListingDays)").Should().BeGreaterThan(0);
        Index(still, "listing.IsListed = false;").Should().BeLessThan(Index(still, "DB.Set(listing);"));
    }

    [Test]
    public void DroidPostings_SaveTheControllerMarkerBeforeThePosting()
    {
        var post = Between(ReadSource("Service", "SpaceDroidCrew.cs").Replace("\r\n", "\n"),
            "public static bool PostDroid(", "var postName = ShipCrewSeats.GetPostName(seat);");
        Index(post, "Droid.MarkPosted(controller, dbShip.Id);").Should().BeLessThan(Index(post, "ExportSingleCharacter(player);"));
        Index(post, "ExportSingleCharacter(player);").Should().BeLessThan(Index(post, "DB.Set(dbShip);"),
            "a restart in between leaves a marker the login check clears, not a posting with a free droid");

        var staff = ReadSource("Service", "PlanetaryHoldings.cs").Replace("\r\n", "\n");
        staff = staff[Index(staff, "joined the staff.\");")..];
        Index(staff, "Droid.MarkPosted(controller, HoldingRules.GetDroidPostingId(holding.Id));")
            .Should().BeLessThan(Index(staff, "ExportSingleCharacter(player);"));
        Index(staff, "ExportSingleCharacter(player);").Should().BeLessThan(Index(staff, "DB.Set(holding);"));
    }

    [Test]
    public void AMarketSearch_ExpiresOldShipListingsFirst()
    {
        var search = ReadSource("Feature", "GuiDefinition", "ViewModel", "MarketBuyViewModel.cs").Replace("\r\n", "\n");
        search = Between(search, "private void Search()", "var results = DB.Search(query);");
        Index(search, "ShipTransfer.ExpireOldListings(marketDetail.MarketId);")
            .Should().BeLessThan(Index(search, "var query = new DBQuery<MarketItem>()"),
                "an expired ship is taken off before the search counts and pages its results");

        var expire = Between(ReadSource("Service", "ShipTransfer.cs").Replace("\r\n", "\n"),
            "public static void ExpireOldListings(", "private static bool IsStillListed(");
        expire.Should().Contain("(int)MarketCategoryType.Starship");
        expire.Should().Contain("IsStillListed(listing);");
    }

    [Test]
    public void ACourierContract_IsSavedBeforeItsSealedLoad()
    {
        var load = Between(ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n"),
            "public static string LoadAgentCourier(", "public static bool HasCourierRoom(");
        Index(load, "DB.Set(dbPlayer);").Should().BeLessThan(Index(load, "ship.SealedFreight.Add("),
            "a restart in between leaves a contract to abandon, not a load no contract can clear");
        Index(load, "ship.SealedFreight.Add(").Should().BeLessThan(Index(load, "DB.Set(ship);"));
    }

    [Test]
    public void SpilledCargo_StaysAboardWhenItsPodCannotBeCreated()
    {
        var source = ReadSource("Service", "TradeCargoSpill.cs").Replace("\r\n", "\n");
        var spill = Between(source, "public static void SpillCargo(", "public static int SpillUnits(");
        Index(spill, "var spilled = SpillUnits(lots.ToDictionary(x => x.Key, x => x.Value.Units), location, kept);")
            .Should().BeLessThan(Index(spill, "CargoHoldRules.AddUnits(ship.Cargo, commodity, units, cost);"));
        var units = Between(source, "public static int SpillUnits(", "public static void ScoopCargoPod()");
        Index(units, "if (!GetIsObjectValid(pod))").Should().BeLessThan(Index(units, "kept[commodity] = current + count;"));
    }

    [Test]
    public void AgentMissionPay_IsMarkedAndACompletedUnpaidMissionIsPaidAtLogin()
    {
        var source = ReadSource("Service", "FactionAgents.cs").Replace("\r\n", "\n");
        var pay = Between(source, "public static void PayMission(", "private static void FailMission(");
        pay.Should().Contain("if (mission == null || mission.Paid)", "a mission already paid is not paid twice");
        Index(pay, "Faction.AdjustPlayerFactionStanding(").Should().BeLessThan(Index(pay, "x => x.PaidStanding = true"));
        Index(pay, "Currency.GiveCurrency(").Should().BeLessThan(Index(pay, "x => x.PaidTokens = true"));
        Index(pay, "GiveGoldToCreature(player, mission.Credits);").Should().BeLessThan(Index(pay, "ExportSingleCharacter(player);"));
        Index(pay, "ExportSingleCharacter(player);").Should().BeLessThan(Index(pay, "x => x.PaidCredits = true"));
        pay.Should().Contain("if (!mission.PaidStanding)").And.Contain("if (!mission.PaidTokens)").And.Contain("if (!mission.PaidCredits)",
            "a retried pay gives only the parts not yet saved");

        var login = Between(source, "public static void RegisterHeldMissions()", "// The board");
        login.Should().Contain("!x.Paid && dbPlayer.Quests.TryGetValue(x.Id, out var quest) && quest.DateLastCompleted != null");
        Index(login, "PayMission(player, missionId);").Should().BeLessThan(Index(login, "OnMissionCompleted(player, missionId);"));
    }

    [Test]
    public void TheTokenStore_CreatesTheItemBeforeTakingTokens()
    {
        var buy = Between(ReadSource("Service", "FactionAgents.cs").Replace("\r\n", "\n"),
            "public static void Buy(uint player, uint agentNpc, int itemIndex)", "public static string GetStoreItemName(");
        Index(buy, "if (!GetIsObjectValid(created))").Should().BeLessThan(Index(buy, "Currency.TakeCurrency(player, currency, item.Tokens);"),
            "a full inventory refuses the purchase before any tokens are spent");
    }

    [Test]
    public void UnregisteringAShip_ReturnsItsPostedDroids()
    {
        var unregister = Between(ReadSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs").Replace("\r\n", "\n"),
            "public Action OnClickUnregisterShip()", "_myShips.RemoveAll(ship => ship.Id == shipId);");
        Index(unregister, "Space.ReturnShipDroidCrew(dbShip, \"was unregistered\");")
            .Should().BeLessThan(Index(unregister, "DB.Delete<PlayerShip>(shipId);"),
                "posted droids go back to their owners before the ship record is gone");
    }

    [Test]
    public void ListingAShip_RechecksItsOwner()
    {
        var list = Between(ReadSource("Service", "ShipTransfer.cs").Replace("\r\n", "\n"),
            "public static bool List(uint seller, PlayerShip ship, int price)", "RemoveListings(ship.Id);");
        Index(list, "if (ship.OwnerPlayerId != GetObjectUUID(seller))").Should().BeGreaterThan(0,
            "a ship given away while the confirmation was open cannot be listed by its former owner");
    }

    [Test]
    public void LeavingSpace_ClearsTheSensorSweepCooldown()
    {
        var clear = ReadSource("Service", "SpaceSignatureJump.cs").Replace("\r\n", "\n");
        clear = clear[Index(clear, "private static void ClearScanningState(")..];
        clear.Should().Contain("_lastSensorSweep.Remove(player);");
    }

    [Test]
    public void YieldThatFitsNowhere_IsKeptInTheHoldOrAtThePilot()
    {
        var source = ReadSource("Service", "ShipHold.cs").Replace("\r\n", "\n");
        var deliver = Between(source, "public static void DeliverYield(", "private static bool TryGetHold(");
        deliver.Should().Contain("if (!GetIsObjectValid(CreateItemOnObject(resref, pilot)))");
        Index(deliver, "unplaced.Add((resref, trait));").Should().BeLessThan(Index(deliver, "KeepUnplacedYield(pilot, unplaced);"));

        var keep = Between(source, "private static void KeepUnplacedYield(", "private static void SaveNewHold(");
        keep.Should().Contain("AddNewStack(hold, resref, Math.Min(left, maxStack));");
        keep.Should().Contain("DB.Set(hold);");
        keep.Should().Contain("CreateObject(ObjectType.Item, resref, location);", "without a hold the yield is left at the pilot");
    }

    [Test]
    public void AFreightContract_IsSavedBeforeItsSealedLoad()
    {
        var accept = Between(ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n"),
            "public static void AcceptFreight(", "private static bool IsFreightExchangeClosed(");
        Index(accept, "DB.Set(dbPlayer);").Should().BeLessThan(Index(accept, "ship.SealedFreight.Add("),
            "a restart in between leaves a contract to abandon, not a load no contract can clear");
    }

    [Test]
    public void ALostCourier_FailsItsMissionBeforeItsContractGoes()
    {
        var fail = Between(ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n"),
            "private static void FailSealedFreight(", "public static string LoadAgentCourier(");
        Index(fail, "FactionAgents.OnCourierLost(ship.OwnerPlayerId, contract.Id);")
            .Should().BeLessThan(Index(fail, "dbPlayer.FreightContracts.RemoveAll(x => lost.Contains(x.Id))"));
        Index(fail, "dbPlayer = DB.Get<Player>(ship.OwnerPlayerId);")
            .Should().BeLessThan(Index(fail, "dbPlayer.FreightContracts.RemoveAll(x => lost.Contains(x.Id))"),
                "the record is reloaded after the courier callbacks saved it");
    }

    [Test]
    public void AStationJump_JoinsTheNewStarportBeforeLeavingTheOld()
    {
        var move = ReadSource("Service", "StationBuildings.cs").Replace("\r\n", "\n");
        move = move[Index(move, "private static void MoveDock(")..];
        Index(move, "DB.Set(starport);").Should().BeLessThan(Index(move, "DB.Set(ship);"));
        Index(move, "DB.Set(ship);").Should().BeLessThan(Index(move, "DB.Set(oldStarport);"),
            "a restart part-way through leaves the ship listed at both Starports, never at none");
    }

    private static int Index(string source, string value) => source.IndexOf(value, StringComparison.Ordinal);

    [Test]
    public void AbandonedFreight_UnloadsTheSealedLotBeforeTheContractGoes()
    {
        var abandon = Between(ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n"),
            "public static void AbandonFreight(", "SendMessageToPC(player, ColorToken.Red($\"Contract abandoned.");
        abandon.IndexOf("DB.Set(ship);", StringComparison.Ordinal).Should().BeGreaterThan(0);
        abandon.IndexOf("DB.Set(ship);", StringComparison.Ordinal)
            .Should().BeLessThan(abandon.IndexOf("dbPlayer.FreightContracts.Remove(contract);", StringComparison.Ordinal),
                "a restart in between leaves the contract to abandon again, not a load no contract can clear");
    }

    [Test]
    public void HoldingDeliveries_ArePendingOnTheShipUntilTheHoldingSavesThem()
    {
        var source = ReadSource("Service", "PlanetaryHoldings.cs").Replace("\r\n", "\n");
        var deliver = Between(source, "public static void Deliver(", "private static HoldingAdvanceResult FinishDelivery(");
        deliver.IndexOf("ship.PendingHoldingDeliveryUnits = units;", StringComparison.Ordinal)
            .Should().BeLessThan(deliver.IndexOf("DB.Set(ship);", StringComparison.Ordinal));
        deliver.IndexOf("DB.Set(ship);", StringComparison.Ordinal)
            .Should().BeLessThan(deliver.IndexOf("FinishDelivery(ship, holding);", StringComparison.Ordinal),
                "the units leave the cargo bay and become a pending delivery in one save");

        var finish = Between(source, "private static HoldingAdvanceResult FinishDelivery(", "public static void FinishPendingDeliveries()");
        finish.Should().Contain("holding.LastDeliveryId != ship.PendingHoldingDeliveryId", "a delivery the holding saved is not added twice");
        finish.IndexOf("DB.Set(holding);", StringComparison.Ordinal)
            .Should().BeLessThan(finish.IndexOf("ship.PendingHoldingDeliveryId = string.Empty;", StringComparison.Ordinal));
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleLoad)]\n        public static void FinishPendingDeliveries()");

        new PlayerShip().PendingHoldingDeliveryId.Should().BeEmpty();
        new PlanetaryHolding().LastDeliveryId.Should().BeEmpty();
    }

    [Test]
    public void AnInterruptedShipSale_StillGivesTheBuyerTheShipAndStoragePermissions()
    {
        var reset = Between(ReadSource("Service", "ShipTransfer.cs").Replace("\r\n", "\n"),
            "private static void ResetPermissions(", "// Direct transfer (gifts)");
        reset.Should().Contain("if (existing.Count == 0 && type != PropertyType.Starship && type != PropertyType.Category)",
            "a ship or category whose old rows a restart already deleted still gets the new owner's rights");
        reset.IndexOf("DB.Delete<WorldPropertyPermission>(permission.Id);", StringComparison.Ordinal)
            .Should().BeLessThan(reset.IndexOf("DB.Set(ShipTransferRules.BuildFullPermission(", StringComparison.Ordinal));
    }

    [Test]
    public void ACourierMission_IsCreditedBeforeItsContractAndLoadAreCleared()
    {
        var source = ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n");
        var deliver = Between(source, "public static void DeliverFreight(", "public static void AbandonFreight(");
        deliver.IndexOf("FactionAgents.OnCourierDelivered(player, contract.Id, result.IsLate);", StringComparison.Ordinal)
            .Should().BeLessThan(deliver.IndexOf("UnloadAgentCourier(GetObjectUUID(player), contract.Id);", StringComparison.Ordinal),
                "a restart before the mission is credited leaves the load to deliver again");

        var unload = source[source.IndexOf("public static void UnloadAgentCourier(", StringComparison.Ordinal)..];
        unload.IndexOf("DB.Set(ship);", StringComparison.Ordinal)
            .Should().BeLessThan(unload.IndexOf("dbPlayer.FreightContracts.Remove(contract);", StringComparison.Ordinal),
                "the load leaves the ship before the contract that clears it goes");
    }

    [Test]
    public void AnAbandonedCourier_EndsItsMissionBeforeItsContractGoes()
    {
        var abandon = Between(ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n"),
            "public static void AbandonFreight(", "SendMessageToPC(player, ColorToken.Red($\"Contract abandoned.");
        abandon.IndexOf("FactionAgents.Abandon(player, contract.Id);", StringComparison.Ordinal)
            .Should().BeLessThan(abandon.IndexOf("UnloadAgentCourier(GetObjectUUID(player), contract.Id);", StringComparison.Ordinal),
                "a restart in between leaves the contract to abandon again, not an active mission without its load");
        abandon.IndexOf("UnloadAgentCourier(GetObjectUUID(player), contract.Id);", StringComparison.Ordinal)
            .Should().BeLessThan(abandon.IndexOf("dbPlayer.FreightContracts.Remove(contract);", StringComparison.Ordinal));
    }

    private static string Between(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        return source[from..source.IndexOf(end, from, StringComparison.Ordinal)];
    }

    [Test]
    public void ACapitalBattle_SavesEveryShareAsPendingBeforeAnyoneCollects()
    {
        var pay = Between(ReadSource("Service", "SpaceCapitalBattles.cs").Replace("\r\n", "\n"),
            "private static int PayCapitalBattle()", "/// <summary>A pilot who was offline");
        pay.Should().NotContain("DeliverPayout(", "online pilots collect from the saved pending payouts");
        pay.IndexOf("CapitalBattleRules.AddPending(state.PendingBattlePayouts, playerId, RollPulls(payout));", StringComparison.Ordinal)
            .Should().BeLessThan(pay.IndexOf("SaveIncursions();", StringComparison.Ordinal));
        pay.IndexOf("SaveIncursions();", StringComparison.Ordinal)
            .Should().BeLessThan(pay.IndexOf("CollectPendingPayout(player, state.PendingBattlePayouts, playerId,", StringComparison.Ordinal));
    }

    [Test]
    public void AHoldingCollection_IsNotedWithTheWarehouse_AndLoadedIntoTheShipOnce()
    {
        var source = ReadSource("Service", "PlanetaryHoldings.cs").Replace("\r\n", "\n");
        var collect = Between(source, "public static void Collect(", "private static void FinishCollection(");
        collect.IndexOf("holding.PendingCollectionId = Guid.NewGuid().ToString();", StringComparison.Ordinal)
            .Should().BeLessThan(collect.IndexOf("DB.Set(holding);", StringComparison.Ordinal));
        collect.IndexOf("DB.Set(holding);", StringComparison.Ordinal)
            .Should().BeLessThan(collect.IndexOf("FinishCollection(holding, ship);", StringComparison.Ordinal));
        collect.Should().NotContain("CargoHoldRules.AddUnits(", "the ship loads the units only through the pending collection");

        var finish = Between(source, "private static void FinishCollection(", "public static void FinishPendingCollections()");
        finish.Should().Contain("ship.LastHoldingCollectionId != holding.PendingCollectionId", "a ship that already saved the units is not loaded twice");
        finish.IndexOf("DB.Set(ship);", StringComparison.Ordinal)
            .Should().BeLessThan(finish.IndexOf("holding.PendingCollectionId = string.Empty;", StringComparison.Ordinal));
        finish.Should().Contain("HoldingRules.AddStock(holding.Warehouse, commodity, units);", "a ship that is gone has the units returned");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleLoad)]\n        public static void FinishPendingCollections()");

        new SWLOR.Game.Server.Entity.PlanetaryHolding().PendingCollectionId.Should().BeEmpty("older holdings have no collection pending");
        new SWLOR.Game.Server.Entity.PlayerShip().LastHoldingCollectionId.Should().BeEmpty();
    }

    [Test]
    public void PendingSpoils_AreNotRemovedBeforeTheDelayedCallback()
    {
        var source = ReadSource("Service", "SpaceIncursions.cs").Replace("\r\n", "\n");
        var handler = source[source.IndexOf("public static void DeliverPendingIncursionPayout()")..];
        handler.IndexOf("PendingPayouts.ContainsKey(playerId)").Should().BeGreaterThan(-1);
        handler.IndexOf("IncursionRules.ClaimPending(").Should().BeGreaterThan(handler.IndexOf("DelayCommand(6f"));
        handler[..handler.IndexOf("DelayCommand(6f")].Should().NotContain("PendingPayouts.Remove");
    }
}