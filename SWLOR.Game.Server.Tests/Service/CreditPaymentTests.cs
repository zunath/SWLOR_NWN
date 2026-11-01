using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.FactionAgentService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Credit payments for changes the database saves (station founding, hull refits, customs fines), agent store token
/// purchases and holding refunds: each is saved in the character file with a mark before or with its change, so a
/// restart at any point neither keeps both the credits and the change nor loses either.
/// </summary>
public class CreditPaymentTests
{
    [Test]
    public void AMark_RoundTripsThroughTheCharacterFileLocal()
    {
        var mark = new CreditPaymentMark { Id = "p1", Kind = CreditPaymentKind.HullRefit, RecordId = "ship", Credits = 5000 };
        CreditPaymentMark.TryParse(mark.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(mark);

        var noRecord = new CreditPaymentMark { Id = "p2", Kind = CreditPaymentKind.CustomsFine, RecordId = string.Empty, Credits = 10 };
        CreditPaymentMark.TryParse(noRecord.Serialize(), out var parsedNoRecord).Should().BeTrue();
        parsedNoRecord.RecordId.Should().BeEmpty();

        CreditPaymentMark.TryParse(null, out _).Should().BeFalse();
        CreditPaymentMark.TryParse(string.Empty, out _).Should().BeFalse();
        CreditPaymentMark.TryParse("p1|0|ship|5", out _).Should().BeFalse("a mark names what it pays for");
        CreditPaymentMark.TryParse("p1|2|ship|-5", out _).Should().BeFalse();
        CreditPaymentMark.TryParse("p1|2|ship", out _).Should().BeFalse();
    }

    [Test]
    public void ALogin_KeepsAPaymentOnlyWhenItsChangeWasSaved()
    {
        var refit = new CreditPaymentMark { Id = "p1", Kind = CreditPaymentKind.HullRefit, RecordId = "ship", Credits = 5000 };

        // The credits and mark were exported, but the restart came before the ship saved the new design.
        refit.KeepsPayment(string.Empty).Should().BeFalse();
        refit.KeepsPayment(null).Should().BeFalse("the ship is gone, so the refit never happened");
        refit.KeepsPayment("an older payment").Should().BeFalse();
        // The ship saved the design with the payment's id before the mark was retired.
        refit.KeepsPayment("p1").Should().BeTrue();

        var fineWithoutShip = new CreditPaymentMark { Id = "f1", Kind = CreditPaymentKind.CustomsFine, RecordId = string.Empty, Credits = 100 };
        fineWithoutShip.KeepsPayment(null).Should().BeTrue("a fine that changes no record is complete once the file holds it");

        var founding = new CreditPaymentMark { Id = "s1", Kind = CreditPaymentKind.StationFounding, RecordId = "station", Credits = 100000 };
        founding.KeepsPayment(null).Should().BeTrue("the station existed before it was paid for, so a paid founding is never refunded");
    }

    [Test]
    public void AStationFounding_IsKeptOnlyWhenTheCharacterFileNamesItsPayment()
    {
        var founding = new CreditPaymentMark { Id = "s1", Kind = CreditPaymentKind.StationFounding, RecordId = "station", Credits = 100000 };
        CreditPaymentMark.PaysPendingFounding(founding, "s1").Should().BeTrue();
        CreditPaymentMark.PaysPendingFounding(founding, "s2").Should().BeFalse("another founding stays unpaid and is undone");
        CreditPaymentMark.PaysPendingFounding(null, "s1").Should().BeFalse("a file that never paid undoes the founding");
        CreditPaymentMark.PaysPendingFounding(founding, string.Empty).Should().BeFalse();
        CreditPaymentMark.PaysPendingFounding(new CreditPaymentMark { Id = "s1", Kind = CreditPaymentKind.HullRefit }, "s1").Should().BeFalse();

        new WorldProperty().PendingPaymentId.Should().BeEmpty("older properties wait on no payment");
        new PlayerShip().LastPaymentId.Should().BeEmpty();
        new Player().LastAgentPurchaseId.Should().BeEmpty();
    }

    [Test]
    public void StationFounding_IsSavedPendingThenPaidOnTheFoundersQueue()
    {
        var dialog = Source("Feature", "DialogDefinition", "PlaceStationCoreDialog.cs");
        dialog.Should().NotContain("TakeGoldFromCreature(");
        InOrder(dialog,
            "var station = Property.CreateStation(player, area, model.Item, anchor, paymentId, StationTuning.InitialPrice);",
            "AssignCommand(player, () => CreditPayments.PayStationFounding(player, stationId, mark));");

        var create = Method(Source("Service", "Property.cs"), "public static WorldProperty CreateStation(");
        InOrder(create, "property.PendingPaymentId = pendingPaymentId ?? string.Empty;", "CreateBuilding(");

        var payments = Source("Service", "CreditPayments.cs");
        InOrder(Method(payments, "public static void Take("),
            "TakeGoldFromCreature(mark.Credits, player, true);", "ObjectPlugin.SetString(player, MarkVariable, mark.Serialize(), true);", "ExportSingleCharacter(player);");
        InOrder(Method(payments, "public static void Retire("), "ObjectPlugin.DeleteString(player, MarkVariable);", "ExportSingleCharacter(player);");
        InOrder(Method(payments, "public static void PayStationFounding("),
            "station.PendingPaymentId != mark.Id", "if (GetGold(player) < mark.Credits)", "UndoStationFounding(station, player);",
            "Take(player, mark);", "station.PendingPaymentId = string.Empty;", "DB.Set(station);", "Retire(player);");
        InOrder(Method(payments, "private static void UndoStationFounding("), "station.IsQueuedForDeletion = true;", "DB.Set(station);");
        InOrder(Method(payments, "private static void SettlePendingFoundings("),
            "CreditPaymentMark.PaysPendingFounding(mark, station.PendingPaymentId)", "UndoStationFounding(station, OBJECT_INVALID);");
        var settle = Method(payments, "public static void SettleInterruptedPayment(");
        InOrder(settle, "SettlePendingFoundings(player, mark);", "if (!mark.KeepsPayment(recordPaymentId))",
            "GiveGoldToCreature(player, mark.Credits);", "Retire(player);");
        payments.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void SettleInterruptedPayment()");
    }

    [Test]
    public void HullRefits_SaveTheDesignWithThePaymentsId()
    {
        var viewModel = Source("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs");
        var click = Method(viewModel, "public Action OnClickRefitDesign()");
        click.Should().NotContain("TakeGoldFromCreature").And.NotContain("DB.Set(dbShip);", "the modal never saves the ship it read earlier");
        click.Should().Contain("AssignCommand(player, () => CompleteRefit(player, shipId, appearance, fee, designName));");

        InOrder(Method(viewModel, "private void CompleteRefit("),
            "var dbShip = DB.Get<PlayerShip>(shipId);", "if (GetGold(player) < fee)",
            "CreditPayments.Take(player, mark);", "dbShip.LastPaymentId = mark.Id;", "DB.Set(dbShip);", "CreditPayments.Retire(player);");
    }

    [Test]
    public void CustomsFines_SaveTheConfiscationWithThePaymentsId()
    {
        var customs = Source("Service", "TradeCustoms.cs");
        customs.Should().NotContain("TakeGoldFromCreature(", "every fine goes through CollectCustomsFine");
        Method(customs, "public static void PayCustomsFine(").Should().Contain("var (fine, taken) = CollectCustomsFine(player, scan.Authority, scan.Fine, true);");
        Method(customs, "public static void ResolveCustomsSpoof(").Should().Contain("CollectCustomsFine(player, scan.Authority, outcome.Fine, outcome.ConfiscatesGoods);");
        InOrder(Method(customs, "private static (int Fine, Dictionary<CommodityType, int> Taken) CollectCustomsFine("),
            "fine = Math.Max(0, Math.Min(fine, GetGold(player)));",
            "CreditPayments.Take(player, mark);",
            "CustomsRules.Confiscate(ship.Cargo, authority)",
            "ship.LastPaymentId = mark.Id;",
            "DB.Set(ship);",
            "CreditPayments.Retire(player);");
    }

    [Test]
    public void AgentStorePurchases_ChargeTheirTokensOnce()
    {
        var purchase = new AgentPurchaseMark { Id = "a1", Currency = CurrencyType.Invalid, Tokens = 30 };
        AgentPurchaseMark.TryParse(purchase.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(purchase);
        AgentPurchaseMark.TryParse("a1|1", out _).Should().BeFalse();
        AgentPurchaseMark.TryParse("a1|1|-2", out _).Should().BeFalse();
        purchase.IsUnpaid(string.Empty).Should().BeTrue("a restart came before the tokens were taken");
        purchase.IsUnpaid("a1").Should().BeFalse("the record names the purchase, so its tokens were taken");

        var agents = Source("Service", "FactionAgents.cs");
        InOrder(Method(agents, "public static void Buy(uint player, uint agentNpc, int itemIndex)"),
            "var created = CreateItemOnObject(item.Resref, player, item.Quantity);",
            "ObjectPlugin.SetString(player, AgentPurchaseVariable, purchase.Serialize(), true);",
            "ExportSingleCharacter(player);",
            "Currency.TakeCurrency(player, currency, item.Tokens, x => x.LastAgentPurchaseId = purchase.Id);");
        InOrder(Method(agents, "public static void ChargeInterruptedAgentPurchase("),
            "!purchase.IsUnpaid(dbPlayer.LastAgentPurchaseId)",
            "Currency.TakeCurrency(player, purchase.Currency, purchase.Tokens, x => x.LastAgentPurchaseId = purchase.Id);");
        Source("Service", "Currency.cs").Should().Contain("if (newBalance == balance && beforeSave == null)",
            "the purchase's id is saved even when the balance is already spent, so it is never charged again later");
    }

    [Test]
    public void AHoldingsRefund_IsSavedBeforeTheHoldingIsDeleted()
    {
        var holdings = Source("Service", "PlanetaryHoldings.cs");
        InOrder(Method(holdings, "public static void Abandon("),
            "GiveGoldToCreature(player, refund);",
            "ObjectPlugin.SetString(player, AbandonedVariable, holding.Id, true);",
            "ExportSingleCharacter(player);",
            "DB.Delete<PlanetaryHolding>(holding.Id);");
        InOrder(Method(holdings, "private static void SettleAbandonedHolding("),
            "holding.OwnerPlayerId != GetObjectUUID(player)", "DB.Delete<PlanetaryHolding>(holding.Id);");
        InOrder(Method(holdings, "public static void CheckHoldingsAtLogin("),
            "SettleAbandonedHolding(player);", "SettlePendingPayments(player);", "var holdings = LoadHoldings(player);");
    }

    [Test]
    public void SpaceRevampDebits_AllRunJournaledOnThePayersQueue()
    {
        foreach (var path in new[]
                 {
                     new[] { "Feature", "DialogDefinition", "PlaceStationCoreDialog.cs" },
                     new[] { "Service", "TradeCustoms.cs" },
                     new[] { "Service", "PlanetaryHoldings.cs" },
                     new[] { "Service", "ShipTransfer.cs" },
                 })
        {
            Source(path).Should().NotContain("() => TakeGoldFromCreature(", string.Join("/", path));
        }

        Method(Source("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs"), "public Action OnClickRefitDesign()")
            .Should().NotContain("TakeGoldFromCreature(");
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
