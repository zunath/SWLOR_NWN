using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Ship transfers and market sales (plan section 7, built in the gaps pass W11d).</summary>
public class ShipTransferTests
{
    private static ShipTransferCheck Clear() => new() { RecipientShipCount = 1 };

    [Test]
    public void AShipDockedAndEmpty_CanChangeOwner()
    {
        ShipTransferRules.GetRefusal(Clear(), false).Should().Be(ShipTransferRefusal.None);
        ShipTransferRules.GetRefusal(Clear(), true).Should().Be(ShipTransferRefusal.None);
        ShipTransferRules.GetRefusal(null, false).Should().Be(ShipTransferRefusal.MissingShip);
    }

    [Test]
    public void EveryBlockingCondition_RefusesWithItsOwnMessage()
    {
        var cases = new (Action<ShipTransferCheck> Set, ShipTransferRefusal Expected)[]
        {
            (c => c.RecipientIsOwner = true, ShipTransferRefusal.SameOwner),
            (c => c.InSpace = true, ShipTransferRefusal.InSpace),
            (c => c.DockedAway = true, ShipTransferRefusal.DockedAway),
            (c => c.PlayersAboard = 1, ShipTransferRefusal.PlayersAboard),
            (c => c.ListedForSale = true, ShipTransferRefusal.ListedForSale),
            (c => c.CargoLots = 1, ShipTransferRefusal.CargoAboard),
            (c => c.SealedFreightLots = 1, ShipTransferRefusal.SealedFreight),
            (c => c.HasUntradeableItem = true, ShipTransferRefusal.UntradeableItem),
            (c => c.RecipientShipCount = Space.MaxRegisteredShips, ShipTransferRefusal.RecipientAtLimit),
        };

        var messages = new HashSet<string>();
        foreach (var (set, expected) in cases)
        {
            var check = Clear();
            set(check);
            ShipTransferRules.GetRefusal(check, false).Should().Be(expected);
            var message = ShipTransferRules.GetRefusalMessage(expected);
            message.Should().NotBeNullOrWhiteSpace();
            messages.Add(message).Should().BeTrue(expected + " needs its own message");
            message.Should().MatchRegex("^[\\x20-\\x7E]+$", "game text is ASCII");
        }

        ShipTransferRules.GetRefusalMessage(ShipTransferRefusal.None).Should().BeEmpty();
    }

    [Test]
    public void ASale_SkipsOnlyItsOwnListing()
    {
        var listed = Clear();
        listed.ListedForSale = true;
        ShipTransferRules.GetRefusal(listed, true).Should().Be(ShipTransferRefusal.None, "the listing is what is being bought");
        ShipTransferRules.GetRefusal(listed, false).Should().Be(ShipTransferRefusal.ListedForSale, "a listed ship is locked");

        listed.PlayersAboard = 2;
        ShipTransferRules.GetRefusal(listed, true).Should().Be(ShipTransferRefusal.PlayersAboard, "a sale is checked again when bought");
    }

    [Test]
    public void TheRecipientMayBeBelowTheShipsRank_ButNotAtTheShipLimit()
    {
        var check = Clear();
        check.RecipientShipCount = Space.MaxRegisteredShips - 1;
        ShipTransferRules.GetRefusal(check, false).Should().Be(ShipTransferRefusal.None);
        check.RecipientShipCount = Space.MaxRegisteredShips;
        ShipTransferRules.GetRefusal(check, true).Should().Be(ShipTransferRefusal.RecipientAtLimit);
    }

    [Test]
    public void TheTransferFee_GrowsWithHullSize()
    {
        var fees = new[] { ShipSize.Small, ShipSize.Medium, ShipSize.Large, ShipSize.Capital }
            .Select(ShipTransferRules.GetTransferFee).ToList();
        fees.Should().BeInAscendingOrder().And.OnlyContain(x => x > 0);
        ShipTransferRules.GetTransferFee(ShipSize.Invalid).Should().Be(fees[0]);
    }

    [Test]
    public void EveryStructureInside_IsReowned_ButNotTheStarportTheShipIsRegisteredAt()
    {
        var (ship, records) = BuildShip("old");
        var owned = ShipTransferRules.CollectOwnedProperties(ship.PropertyId, id => records.GetValueOrDefault(id));

        owned.Select(x => x.Id).Should().BeEquivalentTo(new[] { "ship", "chair", "table", "shed", "shed_interior", "shed_lamp" });
        owned.Should().NotContain(x => x.Id == "starport");

        ShipTransferRules.Reown(ship, owned, "new");
        ship.OwnerPlayerId.Should().Be("new");
        owned.Should().OnlyContain(x => x.OwnerPlayerId == "new");
        records["starport"].OwnerPlayerId.Should().Be("mayor", "a starport registration is not the ship's");
    }

    [Test]
    public void TheMigratedInteriorAndLook_StayAsTheyAre_WhileHotbarsReset()
    {
        var (ship, records) = BuildShip("old");
        ship.HullDesignAppearance = 2002;
        ship.PlayerHotBars["old"] = "hotbar";
        var interior = records["ship"];
        var layout = interior.Layout;
        var chairPosition = records["chair"].Positions[PropertyLocationType.StaticPosition];

        ShipTransferRules.Reown(ship, ShipTransferRules.CollectOwnedProperties(ship.PropertyId, id => records.GetValueOrDefault(id)), "new");

        ship.HullDesignAppearance.Should().Be(2002, "the ship keeps its current look");
        ship.PlayerHotBars.Should().BeEmpty();
        interior.Layout.Should().Be(layout, "a migrated interior keeps its layout");
        interior.CustomName.Should().Be("Old Faithful");
        records["chair"].Positions[PropertyLocationType.StaticPosition].Should().BeSameAs(chairPosition);
        ship.Status.LowPowerModules.Should().ContainKey(1, "fitted modules stay");
    }

    [Test]
    public void PermissionsReset_ToFullRightsForTheNewOwner()
    {
        var permissions = new[] { PropertyPermissionType.EditStructures, PropertyPermissionType.RetrieveStructures };
        var record = ShipTransferRules.BuildFullPermission("ship", "new", permissions);

        record.PropertyId.Should().Be("ship");
        record.PlayerId.Should().Be("new");
        record.Permissions.Should().OnlyContain(x => x.Value).And.HaveCount(2);
        record.GrantPermissions.Should().OnlyContain(x => x.Value).And.HaveCount(2);
    }

    [Test]
    public void Complete_ResetsEveryPermissionAndHandsOffDockedShipsDroidsAndListings()
    {
        var source = ExtractMethod(ReadServerSource("Service", "ShipTransfer.cs"), "public static void Complete(");
        source.Should().Contain("Space.UndockAllToStarport(ship, reason);", "W8c: ships docked aboard go to the starport first");
        source.Should().Contain("Space.ReturnShipDroidCrew(ship, reason);", "droids posted aboard go back to their owners");
        source.Should().Contain("RemoveListings(ship.Id);");
        source.Should().Contain("ResetPermissions(property.Id, property.PropertyType, newOwnerPlayerId);");
        source.Should().Contain("ResetPermissions(category.Id, PropertyType.Category, newOwnerPlayerId);");
        source.Should().Contain("dbOldOwner.ActiveShipId = Guid.Empty.ToString();");
        source.Should().NotContain("UnlockedHullDesigns", "Hull Designs don't change hands");

        var reset = ExtractMethod(ReadServerSource("Service", "ShipTransfer.cs"), "private static void ResetPermissions(");
        reset.Should().Contain("DB.Delete<WorldPropertyPermission>(permission.Id);", "the old owner and their guests lose access");
    }

    [Test]
    public void TheRefit_OnlyOffersDesignsTheRefittingPlayerUnlocked()
    {
        var refit = ExtractMethod(ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs"),
            "private void CompleteRefit(");
        refit.Should().Contain("var dbPlayer = DB.Get<Player>(GetObjectUUID(player));");
        refit.Should().Contain("dbPlayer.UnlockedHullDesigns");
    }

    [Test]
    public void TransfersAndSales_AreCheckedAgainWhenTheyComplete()
    {
        var source = ReadServerSource("Service", "ShipTransfer.cs");
        ExtractMethod(source, "public static void Accept(").Should().Contain("CanCompleteGift(giver, recipient, offer, out _)");
        ExtractMethod(source, "private static void CompleteGift(").Should().Contain("CanCompleteGift(giver, recipient, offer, out var ship)",
            "the giver's queue checks the gift again before it pays");
        ExtractMethod(source, "private static bool CanCompleteGift(").Should().Contain("ShipTransferRules.GetRefusal(BuildCheck(ship, offer.RecipientPlayerId), false)");
        ExtractMethod(source, "public static bool Buy(").Should().Contain("CanBuy(buyer, listing, out _)");
        ExtractMethod(source, "private static void CompletePurchase(").Should().Contain("CanBuy(buyer, listing, out var ship)",
            "the buyer's queue checks the purchase again before it pays");
        ExtractMethod(source, "private static bool CanBuy(").Should().Contain("ShipTransferRules.GetRefusal(BuildCheck(ship, buyerId), true)");
        ExtractMethod(source, "private static void CompletePurchase(").Should().Contain("ship.SaleProceeds = (int)(listing.Price - listing.Price * taxRate);");
        ExtractMethod(source, "public static void Offer(").Should().Contain("GetArea(recipient) != dockArea");
    }

    [Test]
    public void ASaleOwesItsProceedsWithTheNewOwner_AndPaysThemOnce()
    {
        var source = ReadServerSource("Service", "ShipTransfer.cs");
        var buy = ExtractMethod(source, "private static void CompletePurchase(");
        buy.IndexOf("ship.SaleId = payment.Id;", StringComparison.Ordinal)
            .Should().BeLessThan(buy.IndexOf("Complete(ship, buyerId, \"was sold\", payment.Id);", StringComparison.Ordinal),
                "the proceeds owed are saved in the same ship record as the new owner");
        buy.IndexOf("PaySaleProceeds(ship);", StringComparison.Ordinal)
            .Should().BeGreaterThan(buy.IndexOf("Complete(ship, buyerId, \"was sold\", payment.Id);", StringComparison.Ordinal));

        var pay = ExtractMethod(source, "private static void PaySaleProceeds(");
        pay.Should().Contain("!dbSeller.PaidShipSaleIds.Contains(ship.SaleId)", "a sale already in the till is not paid twice");
        pay.IndexOf("dbSeller.PaidShipSaleIds.Add(ship.SaleId);", StringComparison.Ordinal)
            .Should().BeLessThan(pay.IndexOf("ship.SaleId = string.Empty;", StringComparison.Ordinal),
                "the till is paid before the ship stops owing");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleLoad)]\n        public static void FinishInterruptedTransfers()");

        new PlayerShip().SaleId.Should().BeEmpty("older ship records owe nothing");
        new Player().PaidShipSaleIds.Should().BeEmpty();
    }

    [Test]
    public void AListedShip_IsLocked_AndTheMarketRunsTheTransfer()
    {
        ReadServerSource("Service", "Space.cs").Should().Contain("if (ShipTransfer.IsListedForSale(dbShip.Id))");
        ReadServerSource("Service", "Property.cs").Should().Contain("if (ShipTransfer.IsPropertyListedForSale(propertyId))");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ManageStructuresViewModel.cs")
            .Should().Contain("ShipTransfer.IsPropertyListedForSale(structure.ParentPropertyId)");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "MarketBuyViewModel.cs")
            .Should().Contain("ShipTransfer.Buy(Player, dbItem, PlayerMarket.GetMarketRegion(_regionType).TaxRate)");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs")
            .Should().Contain("ShipTransfer.RemoveListings(shipId);");

        var shipId = typeof(MarketItem).GetProperty(nameof(MarketItem.ShipId))!;
        shipId.GetCustomAttribute<IndexedAttribute>().Should().NotBeNull("ship listings are found by ship");
        new MarketItem().ShipId.Should().BeEmpty("item listings and older records point at no ship");
    }

    [TestCase("12000", 12000)]
    [TestCase(" 1,500 ", 1500)]
    public void AnAskingPrice_IsAWholeNumberAboveZero(string text, int expected)
    {
        ShipTransferRules.TryParsePrice(text, out var price).Should().BeTrue();
        price.Should().Be(expected);
    }

    [TestCase("")]
    [TestCase("0")]
    [TestCase("-5")]
    [TestCase("lots")]
    public void AnAskingPrice_RejectsNonsense(string text)
    {
        ShipTransferRules.TryParsePrice(text, out _).Should().BeFalse();
    }

    [Test]
    public void TheSummary_ListsShipDesignModulesFurnitureStoredItemsAndDock()
    {
        var summary = ShipTransferRules.BuildSummary("Old Faithful", "Striker", "Pattern A", 4, 3, 12, "Veles Starport");
        summary.Should().Contain("Old Faithful").And.Contain("Striker (Pattern A design)").And.Contain("Modules fitted: 4")
            .And.Contain("Furniture: 3").And.Contain("Stored items: 12").And.Contain("Docked at: Veles Starport");
        ShipTransferRules.BuildListingName("Old Faithful", "Striker").Should().Be("Old Faithful (Striker)");
        ShipTransferRules.BuildListingName("Striker", "Striker").Should().Be("Striker");
    }

    private static (PlayerShip Ship, Dictionary<string, WorldProperty> Records) BuildShip(string owner)
    {
        WorldProperty Make(string id, PropertyType type) => new() { Id = id, OwnerPlayerId = owner, PropertyType = type };
        var ship = Make("ship", PropertyType.Starship);
        ship.CustomName = "Old Faithful";
        ship.Layout = PropertyLayoutType.LightFreighter1;
        ship.ChildPropertyIds[PropertyChildType.Structure] = new List<string> { "chair", "table", "shed" };
        ship.ChildPropertyIds[PropertyChildType.RegisteredStarport] = new List<string> { "starport" };
        var chair = Make("chair", PropertyType.Structure);
        chair.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation { X = 1, Y = 2, Z = 3 };
        var shed = Make("shed", PropertyType.Structure);
        shed.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { "shed_interior" };
        var shedInterior = Make("shed_interior", PropertyType.Structure);
        shedInterior.ChildPropertyIds[PropertyChildType.Structure] = new List<string> { "shed_lamp" };
        var starport = Make("starport", PropertyType.Structure);
        starport.OwnerPlayerId = "mayor";
        starport.ChildPropertyIds[PropertyChildType.Starship] = new List<string> { "ship" };

        var records = new[] { ship, chair, Make("table", PropertyType.Structure), shed, shedInterior,
                Make("shed_lamp", PropertyType.Structure), starport }
            .ToDictionary(x => x.Id);
        var playerShip = new PlayerShip { OwnerPlayerId = owner, PropertyId = "ship", Status = new ShipStatus() };
        playerShip.Status.LowPowerModules[1] = new ShipStatus.ShipStatusModule { ItemTag = "sm_cargo_00" };
        return (playerShip, records);
    }

    [Test]
    public void APayment_RoundTripsThroughTheCharacterFileLocal()
    {
        var payment = new ShipTransferPayment { Id = "p1", ShipId = "s1", NewOwnerPlayerId = "buyer", Credits = 250000 };

        ShipTransferPayment.TryParse(payment.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(payment);
        ShipTransferPayment.TryParse(string.Empty, out _).Should().BeFalse();
        ShipTransferPayment.TryParse(null, out _).Should().BeFalse();
        ShipTransferPayment.TryParse("p1|s1|buyer|x", out _).Should().BeFalse();
        ShipTransferPayment.TryParse("p1|s1||100", out _).Should().BeFalse("a payment names the ship's new owner");
        ShipTransferPayment.TryParse("p1|s1|buyer|-5", out _).Should().BeFalse();
    }

    [Test]
    public void APayment_IsKeptAtEveryRestartPointAfterItsTransferWasSaved()
    {
        var payment = new ShipTransferPayment { Id = "p1", ShipId = "s1", NewOwnerPlayerId = "recipient", Credits = 1000 };
        var receipt = new ShipTransferReceipt { Id = "p1", ShipId = "s1", NewOwnerPlayerId = "recipient" };
        new PlayerShip().TransferPaymentId.Should().BeEmpty("older ship records carry no payment");

        // 1. The mark was exported, but the transfer intent was never saved: the credits go back.
        var ship = new PlayerShip { Id = "s1", OwnerPlayerId = "giver" };
        payment.IsSettledBy(null, ship).Should().BeFalse();
        payment.IsSettledBy(null, null).Should().BeFalse("a ship deleted later was never transferred");

        // 2. The intent naming the payment was saved: the module load finishes the transfer and writes the receipt.
        ship.TransferToPlayerId = "recipient";
        ship.TransferFromPlayerId = "giver";
        ship.TransferPaymentId = "p1";
        payment.IsSettledBy(null, ship).Should().BeTrue();

        // 3. The receipt was written but the intent not yet cleared.
        payment.IsSettledBy(receipt, ship).Should().BeTrue();

        // 4. The intent was cleared before the payer's mark was retired, and the recipient passed the ship on before the
        //    payer logged in again: the receipt still keeps the payment.
        ship.TransferToPlayerId = string.Empty;
        ship.TransferPaymentId = string.Empty;
        ship.OwnerPlayerId = "third player";
        payment.IsSettledBy(receipt, ship).Should().BeTrue("who owns the ship now does not matter");
        payment.IsSettledBy(receipt, null).Should().BeTrue("the ship may even be gone");

        // 5. Another transfer of the same ship never settles this payment.
        payment.IsSettledBy(new ShipTransferReceipt { Id = "p2" }, new PlayerShip { TransferPaymentId = "p2" }).Should().BeFalse();
        new ShipTransferPayment { Id = string.Empty }.IsSettledBy(null, new PlayerShip()).Should().BeFalse();
    }

    [Test]
    public void TheReceipt_IsSavedWithTheTransferAndOutlivesThePayersMark()
    {
        var source = ReadServerSource("Service", "ShipTransfer.cs");
        var complete = ExtractMethod(source, "public static void Complete(");
        AssertInOrder(complete,
            "if (string.IsNullOrWhiteSpace(ship.TransferToPlayerId))",
            "ship.TransferPaymentId = paymentId ?? string.Empty;",
            "DB.Set(ship);",
            "ShipTransferRules.Reown(ship, owned, newOwnerPlayerId);",
            "DB.Set(new ShipTransferReceipt",
            "Id = ship.TransferPaymentId,",
            "ship.TransferToPlayerId = string.Empty;",
            "ship.TransferPaymentId = string.Empty;",
            "DB.Set(ship);");
        ExtractMethod(source, "public static void FinishInterruptedTransfers(")
            .Should().Contain("Complete(ship, ship.TransferToPlayerId, ship.TransferReason);",
                "the restart recovery keeps the payment id saved with the intent and writes the receipt");

        AssertInOrder(ExtractMethod(source, "private static void RetirePayment("),
            "ObjectPlugin.DeleteString(payer, PaymentVariable);", "ExportSingleCharacter(payer);", "DB.Delete<ShipTransferReceipt>(paymentId);");
        source.Should().NotContain("ClearPayment(", "every mark is retired together with its receipt");

        var receipt = typeof(ShipTransferReceipt);
        receipt.IsSubclassOf(typeof(EntityBase)).Should().BeTrue("the receipt is its own record, keyed by the payment id");
        new ShipTransferReceipt().ShipId.Should().BeEmpty();
    }

    [Test]
    public void TheBuyersAndGiversQueue_SavesThePaymentBeforeTheTransfer()
    {
        var source = ReadServerSource("Service", "ShipTransfer.cs");
        source.Should().NotContain("AssignCommand(buyer, () => TakeGoldFromCreature", "the buyer's debit is saved with its mark, not left to the next export");
        source.Should().NotContain("AssignCommand(giver, () => TakeGoldFromCreature");
        ExtractMethod(source, "public static bool Buy(").Should().Contain("AssignCommand(buyer, () => CompletePurchase(buyer, listingId, price, taxRate));");
        ExtractMethod(source, "public static void Accept(").Should().Contain("AssignCommand(giver, () => CompleteGift(giver, recipient, offer));");

        var purchase = ExtractMethod(source, "private static void CompletePurchase(");
        AssertInOrder(purchase, "if (GetGold(buyer) < price)", "TakeGoldFromCreature(price, buyer, true);", "SavePayment(buyer, payment);",
            "ship.SaleId = payment.Id;", "Complete(ship, buyerId, \"was sold\", payment.Id);", "PaySaleProceeds(ship);", "RetirePayment(buyer, payment.Id);");
        purchase.Should().Contain("if (listing.Price != price)");

        var gift = ExtractMethod(source, "private static void CompleteGift(");
        AssertInOrder(gift, "TakeGoldFromCreature(offer.Fee, giver, true);", "SavePayment(giver, payment);",
            "Complete(ship, offer.RecipientPlayerId, \"changed owner\", payment.Id);", "RetirePayment(giver, payment.Id);");

        AssertInOrder(ExtractMethod(source, "private static void SavePayment("),
            "ObjectPlugin.SetString(payer, PaymentVariable, payment.Serialize(), true);", "ExportSingleCharacter(payer);");

        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void SettleInterruptedPayment()");
        var settle = ExtractMethod(source, "public static void SettleInterruptedPayment(");
        AssertInOrder(settle, "payment.IsSettledBy(DB.Get<ShipTransferReceipt>(payment.Id), DB.Get<PlayerShip>(payment.ShipId))",
            "GiveGoldToCreature(player, payment.Credits);", "RetirePayment(player, payment.Id);");
    }

    private static void AssertInOrder(string source, params string[] parts)
    {
        var at = -1;
        foreach (var part in parts)
        {
            var next = source.IndexOf(part, at + 1, StringComparison.Ordinal);
            next.Should().BeGreaterThan(at, $"'{part}' comes next");
            at = next;
        }
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0)
                return source.Substring(start, i - start + 1);
        }

        return source[start..];
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
