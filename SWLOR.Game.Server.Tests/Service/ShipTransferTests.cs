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
            "public Action OnClickRefitDesign()");
        refit.Should().Contain("var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));");
        refit.Should().Contain("dbPlayer.UnlockedHullDesigns");
    }

    [Test]
    public void TransfersAndSales_AreCheckedAgainWhenTheyComplete()
    {
        var source = ReadServerSource("Service", "ShipTransfer.cs");
        ExtractMethod(source, "public static void Accept(").Should().Contain("ShipTransferRules.GetRefusal(BuildCheck(ship, offer.RecipientPlayerId), false)");
        ExtractMethod(source, "public static bool Buy(").Should().Contain("ShipTransferRules.GetRefusal(BuildCheck(ship, buyerId), true)");
        ExtractMethod(source, "public static bool Buy(").Should().Contain("ship.SaleProceeds = (int)(listing.Price - listing.Price * taxRate);");
        ExtractMethod(source, "public static void Offer(").Should().Contain("GetArea(recipient) != dockArea");
    }

    [Test]
    public void ASaleOwesItsProceedsWithTheNewOwner_AndPaysThemOnce()
    {
        var source = ReadServerSource("Service", "ShipTransfer.cs");
        var buy = ExtractMethod(source, "public static bool Buy(");
        buy.IndexOf("ship.SaleId = Guid.NewGuid().ToString();", StringComparison.Ordinal)
            .Should().BeLessThan(buy.IndexOf("Complete(ship, buyerId, \"was sold\");", StringComparison.Ordinal),
                "the proceeds owed are saved in the same ship record as the new owner");
        buy.IndexOf("PaySaleProceeds(ship);", StringComparison.Ordinal)
            .Should().BeGreaterThan(buy.IndexOf("Complete(ship, buyerId, \"was sold\");", StringComparison.Ordinal));

        var pay = ExtractMethod(source, "private static void PaySaleProceeds(");
        pay.Should().Contain("!dbSeller.PaidShipSaleIds.Contains(ship.SaleId)", "a sale already in the till is not paid twice");
        pay.IndexOf("dbSeller.PaidShipSaleIds.Add(ship.SaleId);", StringComparison.Ordinal)
            .Should().BeLessThan(pay.IndexOf("ship.SaleId = string.Empty;", StringComparison.Ordinal),
                "the till is paid before the ship stops owing");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleLoad)]\n        public static void PayOwedSaleProceeds()");

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
