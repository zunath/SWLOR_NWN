using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Tests.Service;

public class PazaakMatchTests
{
    private static readonly PazaakCardType[] Deck =
    {
        PazaakCardType.PlusMinus2, PazaakCardType.FlipTwoFour, PazaakCardType.Double, PazaakCardType.Tiebreaker,
        PazaakCardType.Plus1, PazaakCardType.Plus2, PazaakCardType.Plus3, PazaakCardType.Minus1,
        PazaakCardType.Minus2, PazaakCardType.Minus3,
    };

    // Always taking the first choice deals the first four side deck cards as the hand.
    private static PazaakMatch StartMatch()
    {
        var match = new PazaakMatch(new PazaakSeat(1, Deck), new PazaakSeat(2, Deck), _ => 0);
        match.Start(0);
        return match;
    }

    /// <summary>
    /// Replaces a seat's table and the cards it draws next (first in the list is drawn first).
    /// </summary>
    private static void Rig(PazaakMatch match, int seat, int[] table, params int[] nextDraws)
    {
        var target = match.Seats[seat];
        target.Table.Clear();
        target.Table.AddRange(table.Select(x => new PazaakTableCard(x)));
        target.MainDeck.Clear();
        target.MainDeck.AddRange(nextDraws.Reverse());
        if (target.MainDeck.Count == 0)
            target.MainDeck.AddRange(Enumerable.Repeat(1, 40));
    }

    [Test]
    public void StartDealsFourSideCardsAndDrawsForTheFirstPlayer()
    {
        var match = StartMatch();

        match.Seats[0].Hand.Should().Equal(Deck.Take(PazaakCards.HandSize));
        match.Seats[1].Hand.Should().Equal(Deck.Take(PazaakCards.HandSize));
        match.Seats[0].Table.Should().HaveCount(1);
        match.Seats[1].Table.Should().BeEmpty();
        match.CurrentSeat.Should().Be(0);
        match.SetNumber.Should().Be(1);
    }

    [Test]
    public void PlusMinusCardUsesTheChosenSign()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 10 });

        match.PlaySideCard(0, 0, negative: true, out _).Should().BeTrue();

        match.Seats[0].Total.Should().Be(8);
        match.Seats[0].Table[^1].Label.Should().Be("-2");
        match.Seats[0].Hand.Should().NotContain(PazaakCardType.PlusMinus2);
    }

    [Test]
    public void FlipTwoFourFlipsEveryTwoAndFourOnTheTable()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 2, 4, 7 });

        match.PlaySideCard(0, 1, negative: false, out _).Should().BeTrue();

        match.Seats[0].Table.Select(x => x.Value).Should().Equal(-2, -4, 7, 0);
        match.Seats[0].Total.Should().Be(1);
    }

    [Test]
    public void DoubleDoublesTheLastCard()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 3, 6 });

        match.PlaySideCard(0, 2, negative: false, out _).Should().BeTrue();

        match.Seats[0].Total.Should().Be(15);
        match.Seats[0].Table[^1].Label.Should().Be("D+6");
    }

    [Test]
    public void OnlyOneSideCardMayBePlayedEachTurn()
    {
        var match = StartMatch();

        match.PlaySideCard(0, 0, negative: false, out _).Should().BeTrue();
        match.PlaySideCard(0, 0, negative: false, out var error).Should().BeFalse();

        error.Should().Be("You have already played a side card this turn.");
    }

    [Test]
    public void PlayersCannotActOutOfTurn()
    {
        var match = StartMatch();

        match.Stand(1, out var error).Should().BeFalse();
        error.Should().Be("It is not your turn.");
        match.PlaySideCard(1, 0, negative: false, out _).Should().BeFalse();
        match.EndTurn(1, out _).Should().BeFalse();
    }

    [Test]
    public void EndTurnPassesTheTurnAndTheOpponentDraws()
    {
        var match = StartMatch();
        Rig(match, 1, System.Array.Empty<int>(), 5);

        match.EndTurn(0, out _).Should().BeTrue();

        match.CurrentSeat.Should().Be(1);
        match.Seats[1].Table.Select(x => x.Value).Should().Equal(5);
    }

    [Test]
    public void EndingATurnOverTwentyBustsAndTheLoserStartsTheNextSet()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 10, 9, 5 });

        match.EndTurn(0, out _).Should().BeTrue();

        match.LastSetResult.WinnerSeat.Should().Be(1);
        match.LastSetResult.WasBust.Should().BeTrue();
        match.Seats[1].SetsWon.Should().Be(1);
        match.SetNumber.Should().Be(2);
        match.CurrentSeat.Should().Be(0);
        match.Seats[0].Table.Should().HaveCount(1);
    }

    [Test]
    public void SideCardCanSaveABustBeforeTheTurnEnds()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 10, 9, 3 });

        match.PlaySideCard(0, 0, negative: true, out _).Should().BeTrue();
        match.Stand(0, out _).Should().BeTrue();

        match.Seats[0].IsStanding.Should().BeTrue();
        match.Seats[0].Total.Should().Be(20);
        match.SetNumber.Should().Be(1);
    }

    [Test]
    public void APlayerKeepsDrawingWhileTheOpponentStands()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 10, 8 });
        Rig(match, 1, System.Array.Empty<int>(), 4, 3);

        match.Stand(0, out _).Should().BeTrue();
        match.EndTurn(1, out _).Should().BeTrue();

        match.CurrentSeat.Should().Be(1);
        match.Seats[1].Table.Select(x => x.Value).Should().Equal(4, 3);
    }

    [Test]
    public void HigherTotalWinsWhenBothStand()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 10, 8 });
        Rig(match, 1, new[] { 10 }, 9);

        match.Stand(0, out _).Should().BeTrue();
        match.Stand(1, out _).Should().BeTrue();

        match.LastSetResult.WinnerSeat.Should().Be(1);
        match.LastSetResult.Totals.Should().Equal(18, 19);
        match.Seats[1].SetsWon.Should().Be(1);
    }

    [Test]
    public void TiebreakerWinsATiedSet()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 10, 9 });

        match.PlaySideCard(0, 3, negative: false, out _).Should().BeTrue();
        match.Stand(0, out _).Should().BeTrue();
        Rig(match, 1, new[] { 10, 10 });
        match.Stand(1, out _).Should().BeTrue();

        match.LastSetResult.WinnerSeat.Should().Be(0);
        match.Seats[0].SetsWon.Should().Be(1);
    }

    [Test]
    public void TieWithoutTiebreakerAwardsNoSetAndTheOtherPlayerStarts()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 10, 8 });
        Rig(match, 1, new[] { 10 }, 8);

        match.Stand(0, out _).Should().BeTrue();
        match.Stand(1, out _).Should().BeTrue();

        match.LastSetResult.WinnerSeat.Should().Be(-1);
        match.Seats.Sum(x => x.SetsWon).Should().Be(0);
        match.SetNumber.Should().Be(2);
        match.CurrentSeat.Should().Be(1);
    }

    [Test]
    public void FillingTheTableWithoutBustingWinsTheSet()
    {
        var match = StartMatch();
        Rig(match, 0, new[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        Rig(match, 1, System.Array.Empty<int>(), 5);

        match.PlaySideCard(0, 0, negative: false, out _).Should().BeTrue();

        match.LastSetResult.WinnerSeat.Should().Be(0);
        match.LastSetResult.WasFullTable.Should().BeTrue();
    }

    [Test]
    public void WinningThreeSetsWinsTheMatch()
    {
        var match = StartMatch();

        // Seat 0 busts every set. The loser starts the next set, so seat 0 always starts.
        for (var set = 0; set < PazaakMatch.SetsToWin; set++)
        {
            match.CurrentSeat.Should().Be(0);
            Rig(match, 0, new[] { 10, 10, 10 });
            match.EndTurn(0, out _).Should().BeTrue();
        }

        match.IsFinished.Should().BeTrue();
        match.WinnerSeat.Should().Be(1);
        match.Seats[1].SetsWon.Should().Be(PazaakMatch.SetsToWin);
        match.EndTurn(match.CurrentSeat, out var error).Should().BeFalse();
        error.Should().Be("The match is over.");
    }

    [Test]
    public void ForfeitGivesTheMatchToTheOpponent()
    {
        var match = StartMatch();

        match.Forfeit(1);

        match.IsFinished.Should().BeTrue();
        match.WasForfeited.Should().BeTrue();
        match.WinnerSeat.Should().Be(0);
    }

    [Test]
    public void SideCardsAreUsedUpAcrossSets()
    {
        var match = StartMatch();
        match.PlaySideCard(0, 0, negative: false, out _).Should().BeTrue();
        Rig(match, 0, new[] { 10, 10, 10 });
        match.EndTurn(0, out _).Should().BeTrue();

        match.SetNumber.Should().Be(2);
        match.Seats[0].Hand.Should().HaveCount(PazaakCards.HandSize - 1);
    }

    [Test]
    public void MainDeckHasFourOfEachCardFromOneToTen()
    {
        var match = StartMatch();

        var cards = new List<int>(match.Seats[1].MainDeck);
        cards.Should().HaveCount(40);
        cards.GroupBy(x => x).Should().OnlyContain(x => x.Count() == 4);
        cards.Distinct().OrderBy(x => x).Should().Equal(Enumerable.Range(1, 10));
    }

    [Test]
    public void CardCatalogHasTheFullSideCardSet()
    {
        PazaakCards.All.Should().HaveCount(22);
        PazaakCards.All.Select(x => x.Type).Should().OnlyHaveUniqueItems();
        PazaakCards.IsValidSideDeck(PazaakCards.DefaultSideDeck.ToList()).Should().BeTrue();
        PazaakCards.IsValidSideDeck(Deck.Take(9).ToList()).Should().BeFalse();
        PazaakCards.IsValidSideDeck(Deck.Take(9).Append(PazaakCardType.Invalid).ToList()).Should().BeFalse();
    }
}
