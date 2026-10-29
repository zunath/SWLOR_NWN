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
        complete.Should().Contain("CapitalBattleRules.AddPending(state.PendingPayouts, playerId, payout);");
        complete.Should().NotContain("DeliverPayout(");
        complete.IndexOf("EndIncursion(true);", StringComparison.Ordinal)
            .Should().BeLessThan(complete.IndexOf("CollectPendingIncursionPayout(player, playerId);", StringComparison.Ordinal),
                "the incursion's end and its owed spoils are saved before anyone collects");
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
    public void PendingSpoils_AreNotRemovedBeforeTheDelayedCallback()
    {
        var source = ReadSource("Service", "SpaceIncursions.cs").Replace("\r\n", "\n");
        var handler = source[source.IndexOf("public static void DeliverPendingIncursionPayout()")..];
        handler.IndexOf("PendingPayouts.ContainsKey(playerId)").Should().BeGreaterThan(-1);
        handler.IndexOf("IncursionRules.ClaimPending(").Should().BeGreaterThan(handler.IndexOf("DelayCommand(6f"));
        handler[..handler.IndexOf("DelayCommand(6f")].Should().NotContain("PendingPayouts.Remove");
    }
}