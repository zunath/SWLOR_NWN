#nullable enable
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Ghost Protocol gained a Stealth I prerequisite, so the crafting release's player migration gives every owner a
/// free refund: the perk, its unlock and its feat are removed and its 6 SP returned, without touching the player's
/// perk refund cooldown or refund tokens.
/// </summary>
public class GhostProtocolRefundMigrationTests
{
    private const string MigrationName = "_26_ConvertCraftingPerksToRanks";
    private const int StartingSP = 5;
    private const int GhostProtocolPrice = 6;

    private static int PreviousVersion => CharacterMigrations.VersionOf(MigrationName) - 1;

    [TestCase(false, TestName = "Owner without Stealth I is refunded")]
    [TestCase(true, TestName = "Owner with Stealth I is refunded too")]
    public void EveryOwnerGetsAFreeRefund(bool ownsStealth)
    {
        var refundCooldown = DateTime.UtcNow.AddDays(3);
        var record = new Player("ghost-protocol-refund") { Version = PreviousVersion, UnallocatedSP = StartingSP };
        record.Perks[PerkType.GhostProtocol] = 1;
        record.UnlockedPerks[PerkType.GhostProtocol] = DateTime.UtcNow;
        if (ownsStealth)
            record.Perks[PerkType.Stealth] = 1;
        record.DatePerkRefundAvailable = refundCooldown;
        record.Currencies[CurrencyType.PerkRefundToken] = 1;

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.Perks.Should().NotContainKey(PerkType.GhostProtocol);
        upgraded.UnlockedPerks.Should().NotContainKey(PerkType.GhostProtocol);
        upgraded.UnallocatedSP.Should().Be(StartingSP + GhostProtocolPrice);
        if (ownsStealth)
            upgraded.Perks[PerkType.Stealth].Should().Be(1, "only Ghost Protocol is refunded");
        upgraded.DatePerkRefundAvailable.Should().Be(refundCooldown, "the free refund does not use the refund cooldown");
        upgraded.Currencies[CurrencyType.PerkRefundToken].Should().Be(1, "the free refund costs no token");
        upgraded.UpgradeNotices.Should().ContainSingle(notice =>
            notice.Contains("'Ghost Protocol'") && notice.Contains("Stealth I") && notice.Contains($"You reclaimed {GhostProtocolPrice} SP."));
    }

    [Test]
    public void ACharacterWithoutGhostProtocolGetsNoGhostProtocolNotice()
    {
        var record = new Player("no-ghost-protocol") { Version = PreviousVersion, UnallocatedSP = StartingSP };
        record.Perks[PerkType.Stealth] = 2;

        var upgraded = CharacterMigrations.Upgrade(SavedCharacterFile.Create(PreviousVersion), record)!.Record;

        upgraded.UnallocatedSP.Should().Be(StartingSP);
        upgraded.Perks[PerkType.Stealth].Should().Be(2);
        upgraded.UpgradeNotices.Should().NotContain(notice => notice.Contains("Ghost Protocol"));
    }

    [Test]
    public void TheRefundRemovesTheGhostProtocolFeatAndMatchesThePerkPrice()
    {
        var migration = CharacterMigrations.Named(MigrationName);
        var table = (System.Collections.IEnumerable)migration
            .GetField("RefundedPerks", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var entry = table.Cast<object>().Single(row => (PerkType)row.GetType().GetProperty("Type")!.GetValue(row)! == PerkType.GhostProtocol);

        ((FeatType[])entry.GetType().GetProperty("GrantedFeats")!.GetValue(entry)!).Should().Equal(FeatType.GhostProtocol);
        ((int[])entry.GetType().GetProperty("Prices")!.GetValue(entry)!).Should().Equal(GhostProtocolPrice);
    }
}
