using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A city's treasury: spending restricted funds first, restricted income, and every escrow transition returning each
/// portion to the balance it came from.
/// </summary>
public class CityTreasuryRulesTests
{
    private static WorldProperty City(long treasury, long restricted = 0)
    {
        return new WorldProperty
        {
            Id = "city",
            PropertyType = PropertyType.City,
            Treasury = treasury,
            RestrictedTreasury = restricted
        };
    }

    #region Spending

    [Test]
    public void ACityWithNoRestrictedFunds_SpendsFromItsTreasury()
    {
        var city = City(1000);

        var spend = CityTreasuryRules.Spend(city, 400);

        spend.Succeeded.Should().BeTrue();
        spend.Restricted.Should().Be(0);
        spend.Unrestricted.Should().Be(400);
        city.Treasury.Should().Be(600);
    }

    [Test]
    public void Spending_UsesRestrictedFundsFirst()
    {
        var city = City(1000, restricted: 300);

        var spend = CityTreasuryRules.Spend(city, 500);

        spend.Succeeded.Should().BeTrue();
        spend.Restricted.Should().Be(300);
        spend.Unrestricted.Should().Be(200);
        spend.Total.Should().Be(500);
        city.RestrictedTreasury.Should().Be(0);
        city.Treasury.Should().Be(800);
    }

    [Test]
    public void Spending_LessThanTheRestrictedBalance_LeavesTheTreasuryAlone()
    {
        var city = City(1000, restricted: 300);

        CityTreasuryRules.Spend(city, 120);

        city.RestrictedTreasury.Should().Be(180);
        city.Treasury.Should().Be(1000);
    }

    [Test]
    public void Spending_WithRestrictedFirstOff_TakesOnlyTheTreasury()
    {
        var city = City(1000, restricted: 300);

        var spend = CityTreasuryRules.Spend(city, 500, restrictedFirst: false);

        spend.Restricted.Should().Be(0);
        spend.Unrestricted.Should().Be(500);
        city.RestrictedTreasury.Should().Be(300);
        city.Treasury.Should().Be(500);
    }

    [Test]
    public void Spending_MoreThanTheCityCanPay_TakesNothing()
    {
        var city = City(1000, restricted: 300);

        CityTreasuryRules.Spend(city, 1301).Succeeded.Should().BeFalse();
        CityTreasuryRules.Spend(city, 1001, restrictedFirst: false).Succeeded.Should().BeFalse();
        CityTreasuryRules.Spend(city, -5).Succeeded.Should().BeFalse();

        city.Treasury.Should().Be(1000);
        city.RestrictedTreasury.Should().Be(300);
    }

    [Test]
    public void Spending_TheWholeOfBothBalances_IsAllowed()
    {
        var city = City(1000, restricted: 300);

        CityTreasuryRules.Spend(city, 1300).Succeeded.Should().BeTrue();

        city.Treasury.Should().Be(0);
        city.RestrictedTreasury.Should().Be(0);
    }

    [Test]
    public void Spendable_IsBothBalancesUnlessRestrictedFundsAreOff()
    {
        var city = City(1000, restricted: 300);

        CityTreasuryRules.GetSpendable(city).Should().Be(1300);
        CityTreasuryRules.GetSpendable(city, restrictedFirst: false).Should().Be(1000);
        CityTreasuryRules.CanSpend(city, 1300).Should().BeTrue();
        CityTreasuryRules.CanSpend(city, 1301).Should().BeFalse();
    }

    [Test]
    public void RestrictedIncome_JoinsTheRestrictedBalanceOnly()
    {
        var city = City(1000);

        CityTreasuryRules.CreditRestricted(city, 250).Should().BeTrue();
        CityTreasuryRules.CreditRestricted(city, 0).Should().BeFalse();
        CityTreasuryRules.CreditRestricted(city, -10).Should().BeFalse();

        city.RestrictedTreasury.Should().Be(250);
        city.Treasury.Should().Be(1000);
    }

    #endregion

    #region Paying upkeep

    [Test]
    public void PayingUpkeep_SpendsRestrictedFundsFirstAndClearsTheBillAndDeadline()
    {
        var city = City(1000, restricted: 300);
        city.Upkeep = 900;
        city.Dates[PropertyDateType.DisrepairDestruction] = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        CityTreasuryRules.PayUpkeep(city).Should().BeTrue();

        city.Upkeep.Should().Be(0);
        city.RestrictedTreasury.Should().Be(0);
        city.Treasury.Should().Be(400);
        city.Dates.Should().NotContainKey(PropertyDateType.DisrepairDestruction);
    }

    [Test]
    public void PayingUpkeep_IsRefusedWhenTheCityCannotPayTheWholeBill()
    {
        var city = City(500, restricted: 300);
        city.Upkeep = 900;

        CityTreasuryRules.PayUpkeep(city).Should().BeFalse();

        city.Upkeep.Should().Be(900);
        city.Treasury.Should().Be(500);
        city.RestrictedTreasury.Should().Be(300);
    }

    [Test]
    public void PayingUpkeep_WithNothingOwedChangesNothing()
    {
        var city = City(500);

        CityTreasuryRules.PayUpkeep(city).Should().BeFalse();
        city.Treasury.Should().Be(500);
    }

    #endregion

    #region Escrow

    [Test]
    public void PostingEscrow_MovesTheCreditsOutOfBothBalancesAndRemembersEachPortion()
    {
        var city = City(1000, restricted: 300);

        var spend = CityTreasuryRules.PostEscrow(city, "project", 500, restrictedFirst: true);

        spend.Succeeded.Should().BeTrue();
        city.RestrictedTreasury.Should().Be(0);
        city.Treasury.Should().Be(800);
        city.Escrow["project"].Restricted.Should().Be(300);
        city.Escrow["project"].Unrestricted.Should().Be(200);
    }

    [Test]
    public void PostingEscrow_ForABountyTakesOnlyTheOrdinaryTreasury()
    {
        var city = City(1000, restricted: 300);

        CityTreasuryRules.PostEscrow(city, "bounty", 500, restrictedFirst: false).Succeeded.Should().BeTrue();

        city.RestrictedTreasury.Should().Be(300);
        city.Treasury.Should().Be(500);
        city.Escrow["bounty"].Restricted.Should().Be(0);
        city.Escrow["bounty"].Unrestricted.Should().Be(500);
    }

    [Test]
    public void PostingEscrow_IsRefusedWhenTheCityCannotPay()
    {
        var city = City(100, restricted: 50);

        CityTreasuryRules.PostEscrow(city, "bounty", 500, restrictedFirst: false).Succeeded.Should().BeFalse();

        city.Escrow.Should().BeEmpty();
        city.Treasury.Should().Be(100);
        city.RestrictedTreasury.Should().Be(50);
    }

    [Test]
    public void PostingEscrow_TwiceUnderTheSameId_HoldsTheMoneyOnlyOnce()
    {
        var city = City(1000);
        CityTreasuryRules.PostEscrow(city, "prize", 400, restrictedFirst: false);

        CityTreasuryRules.PostEscrow(city, "prize", 400, restrictedFirst: false).Succeeded.Should().BeFalse();

        city.Treasury.Should().Be(600);
        city.Escrow["prize"].Unrestricted.Should().Be(400);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void PostingEscrow_NeedsAnId(string id)
    {
        var city = City(1000);

        CityTreasuryRules.PostEscrow(city, id, 100, restrictedFirst: true).Succeeded.Should().BeFalse();

        city.Treasury.Should().Be(1000);
    }

    [Test]
    public void AnEntryFee_IsHeldForThePlayerAndNeverJoinsEitherBalance()
    {
        var city = City(1000);
        CityTreasuryRules.PostEscrow(city, "tournament", 400, restrictedFirst: false);

        CityTreasuryRules.AddEntryFee(city, "tournament", "alice", 50).Should().BeTrue();
        CityTreasuryRules.AddEntryFee(city, "tournament", "alice", 25).Should().BeTrue();
        CityTreasuryRules.AddEntryFee(city, "tournament", "bob", 50).Should().BeTrue();

        city.Escrow["tournament"].PlayerFees["alice"].Should().Be(75);
        city.Escrow["tournament"].PlayerFees["bob"].Should().Be(50);
        city.Treasury.Should().Be(600);
        city.RestrictedTreasury.Should().Be(0);
    }

    [Test]
    public void AnEntryFee_NeedsAnEscrowEntryAPlayerAndAPositiveAmount()
    {
        var city = City(1000);
        CityTreasuryRules.PostEscrow(city, "tournament", 400, restrictedFirst: false);

        CityTreasuryRules.AddEntryFee(city, "missing", "alice", 50).Should().BeFalse();
        CityTreasuryRules.AddEntryFee(city, "tournament", "", 50).Should().BeFalse();
        CityTreasuryRules.AddEntryFee(city, "tournament", "alice", 0).Should().BeFalse();
        CityTreasuryRules.AddEntryFee(city, "tournament", "alice", -5).Should().BeFalse();

        city.Escrow["tournament"].PlayerFees.Should().BeEmpty();
    }

    [Test]
    public void PayingOutEscrow_HandsBackBothPortionsAndLeavesTheBalancesAlone()
    {
        var city = City(1000, restricted: 300);
        CityTreasuryRules.PostEscrow(city, "project", 500, restrictedFirst: true);

        var release = CityTreasuryRules.PayOutEscrow(city, "project");

        release.Found.Should().BeTrue();
        release.Restricted.Should().Be(300);
        release.Unrestricted.Should().Be(200);
        release.Credits.Should().Be(500);
        city.RestrictedTreasury.Should().Be(0, "a completed project's credits are spent");
        city.Treasury.Should().Be(800);
        city.Escrow.Should().BeEmpty();
    }

    [Test]
    public void PayingOutEscrow_HandsBackEntryFeesForTheCallerToPlace()
    {
        var city = City(1000);
        CityTreasuryRules.PostEscrow(city, "tournament", 400, restrictedFirst: false);
        CityTreasuryRules.AddEntryFee(city, "tournament", "alice", 50);
        CityTreasuryRules.AddEntryFee(city, "tournament", "bob", 70);

        var release = CityTreasuryRules.PayOutEscrow(city, "tournament");

        release.Credits.Should().Be(400);
        release.FeeTotal.Should().Be(120);
        release.PlayerFees.Should().BeEquivalentTo(new System.Collections.Generic.Dictionary<string, long> { ["alice"] = 50, ["bob"] = 70 });
        city.Treasury.Should().Be(600, "fees never joined the treasury");
    }

    [Test]
    public void RefundingEscrow_ReturnsEachPortionToTheBalanceItCameFrom()
    {
        var city = City(1000, restricted: 300);
        CityTreasuryRules.PostEscrow(city, "project", 500, restrictedFirst: true);

        var release = CityTreasuryRules.RefundEscrow(city, "project");

        release.Found.Should().BeTrue();
        city.RestrictedTreasury.Should().Be(300, "restricted income cannot become withdrawable credits");
        city.Treasury.Should().Be(1000);
        city.Escrow.Should().BeEmpty();
    }

    [Test]
    public void RefundingEscrow_OfABountyReturnsItToTheOrdinaryTreasury()
    {
        var city = City(1000, restricted: 300);
        CityTreasuryRules.PostEscrow(city, "bounty", 500, restrictedFirst: false);

        CityTreasuryRules.RefundEscrow(city, "bounty");

        city.Treasury.Should().Be(1000);
        city.RestrictedTreasury.Should().Be(300);
    }

    [Test]
    public void RefundingEscrow_ReturnsEntryFeesInTheReleaseNotTheTreasury()
    {
        var city = City(1000);
        CityTreasuryRules.PostEscrow(city, "tournament", 400, restrictedFirst: false);
        CityTreasuryRules.AddEntryFee(city, "tournament", "alice", 50);

        var release = CityTreasuryRules.RefundEscrow(city, "tournament");

        release.PlayerFees.Should().ContainKey("alice").WhoseValue.Should().Be(50);
        city.Treasury.Should().Be(1000, "the prize came back, and the fee goes to Alice");
    }

    [Test]
    public void ReleasingEscrow_TwiceFindsNothingTheSecondTime()
    {
        var city = City(1000, restricted: 300);
        CityTreasuryRules.PostEscrow(city, "project", 500, restrictedFirst: true);
        CityTreasuryRules.RefundEscrow(city, "project");

        var again = CityTreasuryRules.RefundEscrow(city, "project");
        var payOut = CityTreasuryRules.PayOutEscrow(city, "project");

        again.Found.Should().BeFalse();
        payOut.Found.Should().BeFalse();
        city.Treasury.Should().Be(1000, "a repeated refund cannot return the money twice");
        city.RestrictedTreasury.Should().Be(300);
    }

    [Test]
    public void ReleasingAnUnknownEscrow_ChangesNothing()
    {
        var city = City(1000, restricted: 300);

        CityTreasuryRules.RefundEscrow(city, "nothing").Found.Should().BeFalse();
        CityTreasuryRules.PayOutEscrow(city, "nothing").Found.Should().BeFalse();
        CityTreasuryRules.RefundEscrow(city, null).Found.Should().BeFalse();

        city.Treasury.Should().Be(1000);
        city.RestrictedTreasury.Should().Be(300);
    }

    [Test]
    public void SeveralEscrowEntries_KeepTheirPortionsApart()
    {
        var city = City(1000, restricted: 300);
        CityTreasuryRules.PostEscrow(city, "project", 400, restrictedFirst: true);
        CityTreasuryRules.PostEscrow(city, "bounty", 200, restrictedFirst: false);

        CityTreasuryRules.RefundEscrow(city, "bounty");
        city.Treasury.Should().Be(900);
        city.RestrictedTreasury.Should().Be(0);

        CityTreasuryRules.RefundEscrow(city, "project");
        city.Treasury.Should().Be(1000);
        city.RestrictedTreasury.Should().Be(300);
        city.Escrow.Keys.ToList().Should().BeEmpty();
    }

    [Test]
    public void EveryTransition_ConservesTheCreditsTheCityStartedWith()
    {
        var city = City(1000, restricted: 300);
        var total = city.Treasury + city.RestrictedTreasury;

        CityTreasuryRules.PostEscrow(city, "a", 450, restrictedFirst: true);
        CityTreasuryRules.PostEscrow(city, "b", 100, restrictedFirst: false);
        var held = city.Escrow.Values.Sum(x => x.Restricted + x.Unrestricted);
        (city.Treasury + city.RestrictedTreasury + held).Should().Be(total);

        CityTreasuryRules.RefundEscrow(city, "a");
        var payout = CityTreasuryRules.PayOutEscrow(city, "b");
        (city.Treasury + city.RestrictedTreasury + payout.Credits).Should().Be(total);
    }

    #endregion
}
