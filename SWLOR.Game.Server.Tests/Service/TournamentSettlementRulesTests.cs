using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DuelService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Who is owed what when a tournament ends, and that the money in the city's escrow is accounted for on every ending: the
/// prize goes to the champion, a prize that is not won goes back to the balance it came from, and entry fees go to the
/// city when the final ends and back to the entrants on a cancellation or a removed city.
/// </summary>
public class TournamentSettlementRulesTests
{
    private const long Prize = 50_000;
    private const long Fee = 1_000;

    private static CityTournament Tournament(int entrants, long fee = Fee, string champion = "p1")
    {
        var tournament = new CityTournament
        {
            Id = "tournament-1",
            CityId = "city-1",
            State = TournamentState.Settling,
            Prize = Prize,
            EntryFee = fee,
            ChampionPlayerId = champion
        };

        for (var i = 1; i <= entrants; i++)
            tournament.Entrants.Add(new TournamentEntrant { PlayerId = $"p{i}", FeePaid = fee });

        return tournament;
    }

    private static CityEscrowEntry Escrow(CityTournament tournament)
    {
        var entry = new CityEscrowEntry { Unrestricted = tournament.Prize };
        foreach (var entrant in tournament.Entrants.Where(x => x.FeePaid > 0))
            entry.PlayerFees[entrant.PlayerId] = entrant.FeePaid;

        return entry;
    }

    // A completed tournament

    [Test]
    public void ACompletedTournament_PaysThePrizeToTheChampion_AndKeepsTheFeesForTheCity()
    {
        var tournament = Tournament(4);

        var settlement = TournamentSettlementRules.Build(tournament, TournamentOutcome.Completed, Escrow(tournament));

        settlement.Payouts.Should().ContainSingle();
        settlement.Payouts[0].PlayerId.Should().Be("p1");
        settlement.Payouts[0].Credits.Should().Be(Prize);
        settlement.FeesToTreasury.Should().Be(4 * Fee);
    }

    [Test]
    public void ACompletedTournament_WithNoPrize_OwesNoOne()
    {
        var tournament = Tournament(2);
        tournament.Prize = 0;

        var settlement = TournamentSettlementRules.Build(tournament, TournamentOutcome.Completed, Escrow(tournament));

        settlement.Payouts.Should().BeEmpty();
        settlement.FeesToTreasury.Should().Be(2 * Fee);
    }

    [Test]
    public void ACompletedFreeTournament_KeepsNoFees()
    {
        var tournament = Tournament(2, fee: 0);

        var settlement = TournamentSettlementRules.Build(tournament, TournamentOutcome.Completed, Escrow(tournament));

        settlement.Payouts.Should().ContainSingle();
        settlement.FeesToTreasury.Should().Be(0);
    }

    [Test]
    public void TheChampion_IsReadFromTheFinalWhenTheTournamentHoldsNone()
    {
        var tournament = Tournament(2, champion: string.Empty);
        tournament.Matches = TournamentRules.BuildBracket(new List<string> { "p1", "p2" });
        TournamentRules.ApplyDuelOutcome(tournament.Matches[0], new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Target), DateTime.UtcNow);

        var settlement = TournamentSettlementRules.Build(tournament, TournamentOutcome.Completed, Escrow(tournament));

        settlement.Payouts.Should().ContainSingle().Which.PlayerId.Should().Be("p2");
    }

    // Cancelled and removed

    [TestCase(TournamentOutcome.Cancelled)]
    [TestCase(TournamentOutcome.CityRemoved)]
    public void ACalledOffTournament_ReturnsEveryEntryFeeToItsEntrant_AndNoFeesStayWithTheCity(TournamentOutcome outcome)
    {
        var tournament = Tournament(3);

        var settlement = TournamentSettlementRules.Build(tournament, outcome, Escrow(tournament));

        settlement.Payouts.Select(x => x.PlayerId).Should().BeEquivalentTo("p1", "p2", "p3");
        settlement.Payouts.Should().OnlyContain(x => x.Credits == Fee);
        settlement.FeesToTreasury.Should().Be(0);
    }

    [Test]
    public void ACalledOffFreeTournament_OwesNoOne()
    {
        var tournament = Tournament(3, fee: 0);

        TournamentSettlementRules.Build(tournament, TournamentOutcome.Cancelled, Escrow(tournament)).Payouts.Should().BeEmpty();
    }

    [Test]
    public void ACalledOffTournament_NeverPaysThePrizeToAnyone()
    {
        var tournament = Tournament(3);

        var settlement = TournamentSettlementRules.Build(tournament, TournamentOutcome.Cancelled, Escrow(tournament));

        settlement.Payouts.Sum(x => x.Credits).Should().Be(3 * Fee, "the prize goes back to the treasury through the escrow refund");
    }

    [Test]
    public void ACityThatIsGone_HasNoEscrowEntry_AndTheEntrantsRecordsStillReturnTheirFees()
    {
        var settlement = TournamentSettlementRules.Build(Tournament(2), TournamentOutcome.CityRemoved, null);

        settlement.Payouts.Should().HaveCount(2);
        settlement.Payouts.Should().OnlyContain(x => x.Credits == Fee);
    }

    // A restart between the two writes of a sign-up

    [Test]
    public void AFeeOnlyTheEntrantRecordNames_IsStillCounted()
    {
        var tournament = Tournament(2);
        var escrow = Escrow(tournament);
        escrow.PlayerFees.Remove("p2");

        var fees = TournamentSettlementRules.GetFeesPaid(tournament, escrow);

        fees["p1"].Should().Be(Fee);
        fees["p2"].Should().Be(Fee);
        TournamentSettlementRules.Build(tournament, TournamentOutcome.Cancelled, escrow).Payouts.Should().HaveCount(2);
    }

    [Test]
    public void AFeeOnlyTheEscrowNames_IsStillTheirs()
    {
        var tournament = Tournament(1);
        var escrow = Escrow(tournament);
        escrow.PlayerFees["ghost"] = 500;

        var fees = TournamentSettlementRules.GetFeesPaid(tournament, escrow);

        fees["ghost"].Should().Be(500);
        fees.Values.Sum().Should().Be(Fee + 500);
    }

    [Test]
    public void WhenTheTwoRecordsDisagree_TheLargerFeeStands_AndNothingIsCountedTwice()
    {
        var tournament = Tournament(1);
        var escrow = Escrow(tournament);
        escrow.PlayerFees["p1"] = Fee * 2;

        TournamentSettlementRules.GetFeesPaid(tournament, escrow)["p1"].Should().Be(Fee * 2);

        escrow.PlayerFees["p1"] = Fee;
        TournamentSettlementRules.GetFeesPaid(tournament, escrow)["p1"].Should().Be(Fee);
    }

    // Writing the same debt twice

    [Test]
    public void ThePayoutIds_AreTheSameEveryTimeTheSettlementIsWorkedOut()
    {
        var tournament = Tournament(3);

        var first = TournamentSettlementRules.Build(tournament, TournamentOutcome.Cancelled, Escrow(tournament));
        var second = TournamentSettlementRules.Build(tournament, TournamentOutcome.Cancelled, Escrow(tournament));

        first.Payouts.Select(x => x.Id).Should().Equal(second.Payouts.Select(x => x.Id));
        first.Payouts.Select(x => x.Id).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void ThePayoutIds_DifferByOutcomeAndPlayer()
    {
        var tournament = Tournament(2);

        TournamentSettlementRules.BuildPayoutId(tournament, TournamentOutcome.Cancelled, "p1")
            .Should().NotBe(TournamentSettlementRules.BuildPayoutId(tournament, TournamentOutcome.Completed, "p1"));
        TournamentSettlementRules.BuildPayoutId(tournament, TournamentOutcome.Cancelled, "p1")
            .Should().NotBe(TournamentSettlementRules.BuildPayoutId(tournament, TournamentOutcome.Cancelled, "p2"));
    }

    // The money, against the real treasury rules

    private static WorldProperty City(long treasury, long restricted = 0)
    {
        return new WorldProperty { Id = "city-1", PropertyType = PropertyType.City, Treasury = treasury, RestrictedTreasury = restricted };
    }

    [Test]
    public void ThePrize_IsHeldOutOfTheOrdinaryTreasuryOnly_NeverRestrictedFunds()
    {
        var city = City(treasury: 80_000, restricted: 500_000);

        CityTreasuryRules.GetSpendable(city, restrictedFirst: false).Should().Be(80_000);

        var spend = CityTreasuryRules.PostEscrow(city, "tournament-1", Prize, restrictedFirst: false);

        spend.Succeeded.Should().BeTrue();
        spend.Restricted.Should().Be(0);
        city.RestrictedTreasury.Should().Be(500_000, "situation income can never become a prize");
        city.Treasury.Should().Be(30_000);
    }

    [Test]
    public void ARestrictedBalance_CannotCoverAPrizeTheTreasuryCannot()
    {
        var city = City(treasury: 10_000, restricted: 500_000);

        CityTreasuryRules.PostEscrow(city, "tournament-1", Prize, restrictedFirst: false).Succeeded.Should().BeFalse();

        city.Treasury.Should().Be(10_000);
        city.Escrow.Should().BeEmpty();
    }

    private static WorldProperty FundedCity(CityTournament tournament, long treasury = 80_000, long restricted = 0)
    {
        var city = City(treasury, restricted);
        CityTreasuryRules.PostEscrow(city, tournament.Id, Prize, restrictedFirst: false);
        foreach (var entrant in tournament.Entrants)
            CityTreasuryRules.AddEntryFee(city, tournament.Id, entrant.PlayerId, entrant.FeePaid);

        return city;
    }

    [Test]
    public void WhenTheFinalEnds_TheChampionIsOwedThePrize_AndTheCityKeepsTheFees()
    {
        var tournament = Tournament(4);
        var city = FundedCity(tournament);

        var settlement = TournamentSettlementRules.Apply(city, tournament, TournamentOutcome.Completed);

        settlement.ReleasedEscrow.Should().BeTrue();
        city.Escrow.Should().BeEmpty();
        city.Treasury.Should().Be(80_000 - Prize + 4 * Fee);
        settlement.Payouts.Sum(x => x.Credits).Should().Be(Prize);
        settlement.Payouts.Single().PlayerId.Should().Be("p1");
    }

    [Test]
    public void WhenTheTournamentIsCancelled_NothingIsCreatedOrLost()
    {
        var tournament = Tournament(4);
        var city = FundedCity(tournament);

        var settlement = TournamentSettlementRules.Apply(city, tournament, TournamentOutcome.Cancelled);

        city.Escrow.Should().BeEmpty();
        city.Treasury.Should().Be(80_000, "the prize is back where it came from");
        settlement.Payouts.Sum(x => x.Credits).Should().Be(4 * Fee, "every fee goes back to the player who paid it");
    }

    [Test]
    public void RestrictedFunds_AreNeverTouchedByAPrize_OnAnyEnding()
    {
        foreach (var outcome in new[] { TournamentOutcome.Completed, TournamentOutcome.Cancelled })
        {
            var tournament = Tournament(3);
            var city = FundedCity(tournament, restricted: 500_000);

            TournamentSettlementRules.Apply(city, tournament, outcome);

            city.RestrictedTreasury.Should().Be(500_000);
        }
    }

    [TestCase(TournamentOutcome.Completed)]
    [TestCase(TournamentOutcome.Cancelled)]
    public void SettlingTwice_AfterARestart_ReleasesTheEscrowAndCreditsTheFeesOnce(TournamentOutcome outcome)
    {
        var tournament = Tournament(4);
        var city = FundedCity(tournament);

        var first = TournamentSettlementRules.Apply(city, tournament, outcome);
        var treasuryAfterFirst = city.Treasury;
        var second = TournamentSettlementRules.Apply(city, tournament, outcome);

        second.ReleasedEscrow.Should().BeFalse("the escrow entry is already gone");
        city.Treasury.Should().Be(treasuryAfterFirst, "the fees are not credited a second time");
        second.Payouts.Select(x => x.Id).Should().Equal(first.Payouts.Select(x => x.Id), "the owed credits keep their ids, so they are written once");
    }

    [Test]
    public void ARestartBeforeTheCityWasSaved_RepeatsTheWholeSettlement()
    {
        var tournament = Tournament(4);
        var saved = FundedCity(tournament);
        var unsavedCopy = City(saved.Treasury);
        unsavedCopy.Escrow[tournament.Id] = saved.Escrow[tournament.Id];

        TournamentSettlementRules.Apply(unsavedCopy, tournament, TournamentOutcome.Completed);

        // The city record that reached the database still holds the escrow entry, so the next run settles it in full.
        var again = TournamentSettlementRules.Apply(saved, tournament, TournamentOutcome.Completed);

        again.ReleasedEscrow.Should().BeTrue();
        saved.Treasury.Should().Be(80_000 - Prize + 4 * Fee);
    }

    // A restart in the middle of signing up, then a cancellation before the entrant logs back in

    private static CityTournament PaidEntrants(int entrants)
    {
        var tournament = Tournament(entrants);
        foreach (var entrant in tournament.Entrants)
            entrant.PaymentId = $"pay-{entrant.PlayerId}";

        return tournament;
    }

    private static CreditPaymentMark EntryMark(string playerId) =>
        new() { Id = $"pay-{playerId}", Kind = CreditPaymentKind.TournamentEntry, RecordId = "tournament-1", Credits = (int)Fee };

    [TestCase(TournamentOutcome.Cancelled)]
    [TestCase(TournamentOutcome.CityRemoved)]
    public void ARestartAfterTheEntrantWasSaved_ThenACancellation_ReturnsTheFeeOnce(TournamentOutcome outcome)
    {
        var tournament = PaidEntrants(3);
        var mark = EntryMark("p2");

        // The server stopped after the entrant was saved and before the mark was retired. The settlement owes p2 the fee...
        var settlement = TournamentSettlementRules.Build(tournament, outcome, Escrow(tournament));
        settlement.Payouts.Where(x => x.PlayerId == "p2").Sum(x => x.Credits).Should().Be(Fee);

        // ...and deletes the tournament, so the entrant's record no longer says the sign-up was saved.
        var recordPaymentId = TournamentRules.FindEntryPaymentId(null, mark.Id);
        mark.KeepsPayment(recordPaymentId).Should().BeFalse("without a receipt the login would return the fee a second time");

        // The receipts the settlement wrote before it deleted the tournament settle the mark.
        var receipts = TournamentSettlementRules.GetEntryReceipts(tournament);
        receipts[mark.Id].Should().Be("p2");
        mark.KeepsPayment(recordPaymentId, receipts.ContainsKey(mark.Id)).Should().BeTrue("the fee is already owed through the settlement");
    }

    [Test]
    public void ARestartAfterTheEntrantWasSaved_ThenACompletedTournament_KeepsTheFeeForTheCity()
    {
        var tournament = PaidEntrants(4);
        var mark = EntryMark("p3");

        TournamentSettlementRules.Build(tournament, TournamentOutcome.Completed, Escrow(tournament))
            .Payouts.Where(x => x.PlayerId == "p3").Should().BeEmpty("the fee went to the city");

        mark.KeepsPayment(TournamentRules.FindEntryPaymentId(null, mark.Id), TournamentSettlementRules.GetEntryReceipts(tournament).ContainsKey(mark.Id))
            .Should().BeTrue("the sign-up was saved, so the fee is not returned");
    }

    [Test]
    public void ARestartBeforeTheEntrantWasSaved_ThenACancellation_StillReturnsTheFeeThroughTheMark()
    {
        var tournament = PaidEntrants(2);
        var unsaved = EntryMark("p9");

        // p9 never reached the tournament, so the settlement owes them nothing and wrote no receipt for them.
        TournamentSettlementRules.Build(tournament, TournamentOutcome.Cancelled, Escrow(tournament)).Payouts.Should().NotContain(x => x.PlayerId == "p9");
        var receipts = TournamentSettlementRules.GetEntryReceipts(tournament);

        receipts.ContainsKey(unsaved.Id).Should().BeFalse();
        unsaved.KeepsPayment(TournamentRules.FindEntryPaymentId(null, unsaved.Id), receipts.ContainsKey(unsaved.Id))
            .Should().BeFalse("the login returns the fee the player was charged, the only time it is returned");
    }

    [Test]
    public void TheReceipts_AreOnePerPaidEntrant_AndNoneForAnEntrantWhoPaidNothing()
    {
        var tournament = PaidEntrants(3);
        tournament.Entrants[1].PaymentId = string.Empty;
        tournament.Entrants[2].PlayerId = string.Empty;

        var receipts = TournamentSettlementRules.GetEntryReceipts(tournament);

        receipts.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("pay-p1", "p1"));
        TournamentSettlementRules.GetEntryReceipts(new CityTournament()).Should().BeEmpty();
    }

    [Test]
    public void ARemovedCity_OnlyOwesTheFees()
    {
        var tournament = Tournament(3);

        var settlement = TournamentSettlementRules.Apply(null, tournament, TournamentOutcome.CityRemoved);

        settlement.ReleasedEscrow.Should().BeFalse();
        settlement.Payouts.Sum(x => x.Credits).Should().Be(3 * Fee);
        settlement.FeesToTreasury.Should().Be(0);
    }
}
