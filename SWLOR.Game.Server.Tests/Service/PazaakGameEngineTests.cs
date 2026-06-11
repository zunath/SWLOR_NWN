using NUnit.Framework;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Tests.Service;

public class PazaakGameEngineTests
{
    private class FixedRandom : IPazaakRandom
    {
        private readonly Queue<int> _values = new();

        public FixedRandom(params int[] values)
        {
            foreach (var value in values)
            {
                _values.Enqueue(value);
            }
        }

        public int Next(int maxValue)
        {
            if (_values.Count <= 0)
                return 0;

            return Math.Clamp(_values.Dequeue(), 0, maxValue - 1);
        }
    }

    [Test]
    public void ValidateSideDeck_RequiresExactlyTenCards()
    {
        var deck = PazaakCardCatalog.StarterDeck.Take(9);

        var result = PazaakGameEngine.ValidateSideDeck(deck);

        Assert.That(result, Does.Contain("exactly 10"));
    }

    [Test]
    public void EndTurn_BustsOnlyWhenTurnEnds()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 10, 9, 2);

        PazaakGameEngine.EndTurn(match, PazaakParticipantSlot.PlayerOne, new FixedRandom());

        Assert.That(match.Participants[1].SetsWon, Is.EqualTo(1));
    }

    [Test]
    public void PlaySideCard_AllowsOnlyOneSideCardPerTurn()
    {
        var match = CreateMatch();
        match.Participants[0].SideHand = new List<PazaakCardType>
        {
            PazaakCardType.Plus1,
            PazaakCardType.Plus2,
        };

        PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 0, new FixedRandom());

        Assert.Throws<InvalidOperationException>(() =>
            PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 0, new FixedRandom()));
    }

    [Test]
    public void Stand_TiedSetStartsNewSetWithoutRedrawingSideHands()
    {
        var match = CreateMatch();
        var firstHand = match.Participants[0].SideHand.ToList();
        var secondHand = match.Participants[1].SideHand.ToList();
        SetBoard(match.Participants[0], 10, 10);
        SetBoard(match.Participants[1], 10, 10);
        match.Participants[1].IsStanding = true;

        PazaakGameEngine.Stand(match, PazaakParticipantSlot.PlayerOne, new FixedRandom());

        Assert.That(match.CurrentSet, Is.EqualTo(2));
        Assert.That(match.Participants[0].SetsWon, Is.EqualTo(0));
        Assert.That(match.Participants[1].SetsWon, Is.EqualTo(0));
        Assert.That(match.Participants[0].SideHand, Is.EqualTo(firstHand));
        Assert.That(match.Participants[1].SideHand, Is.EqualTo(secondHand));
    }

    [Test]
    public void Stand_TieBreakerWinsTiedSet()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 10, 10);
        SetBoard(match.Participants[1], 10, 10);
        match.Participants[0].Board.Add(new PazaakPlayedCard(PazaakCardType.TieBreaker, "Tie -1", -1, false));
        match.Participants[0].Board.Add(new PazaakPlayedCard(PazaakCardType.Plus1, "+1", 1, false));
        match.Participants[1].IsStanding = true;

        PazaakGameEngine.Stand(match, PazaakParticipantSlot.PlayerOne, new FixedRandom());

        Assert.That(match.Participants[0].SetsWon, Is.EqualTo(1));
    }

    [Test]
    public void PlaySideCard_NineCardsAtOrUnderTwentyWinsSet()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 1, 1, 1, 1, 1, 1, 1, 1);
        match.Participants[0].SideHand = new List<PazaakCardType> { PazaakCardType.Plus1 };

        PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 0, new FixedRandom());

        Assert.That(match.Participants[0].SetsWon, Is.EqualTo(1));
    }

    [Test]
    public void ChoiceCards_UseSelectedValue()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 10);
        match.Participants[0].SideHand = new List<PazaakCardType> { PazaakCardType.PlusMinus4 };

        PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, -4, new FixedRandom());

        Assert.That(match.Participants[0].Total, Is.EqualTo(6));
    }

    [Test]
    public void ChoiceCards_InvalidValueDoesNotConsumeSideCard()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 10);
        match.Participants[0].SideHand = new List<PazaakCardType> { PazaakCardType.PlusMinus4 };

        Assert.Throws<ArgumentException>(() =>
            PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 3, new FixedRandom()));

        Assert.That(match.Participants[0].SideHand, Is.EqualTo(new[] { PazaakCardType.PlusMinus4 }));
        Assert.That(match.Participants[0].HasPlayedSideCardThisTurn, Is.False);
    }

    [Test]
    public void GoldCards_ApplyDoubleFlipAndOneOrMinusTwoEffects()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 5);
        match.Participants[0].SideHand = new List<PazaakCardType> { PazaakCardType.Double };

        PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 0, new FixedRandom());

        Assert.That(match.Participants[0].Total, Is.EqualTo(10));

        match = CreateMatch();
        SetBoard(match.Participants[0], 2, -2, 4, -4, 3);
        match.Participants[0].SideHand = new List<PazaakCardType> { PazaakCardType.Flip2And4 };

        PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 0, new FixedRandom());

        Assert.That(match.Participants[0].Total, Is.EqualTo(-9));

        match = CreateMatch();
        SetBoard(match.Participants[0], 10);
        match.Participants[0].SideHand = new List<PazaakCardType> { PazaakCardType.OneOrMinusTwo };

        PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 2, new FixedRandom());

        Assert.That(match.Participants[0].Total, Is.EqualTo(12));
    }

    [Test]
    public void TieBreaker_AddsOrSubtractsOneAndCanWinImmediatelyAgainstStandingTie()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 10, 9);
        SetBoard(match.Participants[1], 10, 10);
        match.Participants[1].IsStanding = true;
        match.Participants[0].SideHand = new List<PazaakCardType> { PazaakCardType.TieBreaker };

        PazaakGameEngine.PlaySideCard(match, PazaakParticipantSlot.PlayerOne, 0, 1, new FixedRandom());

        Assert.That(match.Participants[0].SetsWon, Is.EqualTo(1));
    }

    [Test]
    public void EndTurn_DrawsAgainForSamePlayerWhenOpponentHasStood()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 5);
        SetBoard(match.Participants[1], 18);
        match.Participants[1].IsStanding = true;
        match.MainDeck = new List<int> { 3 };

        PazaakGameEngine.EndTurn(match, PazaakParticipantSlot.PlayerOne, new FixedRandom());

        Assert.That(match.Status, Is.EqualTo(PazaakMatchStatus.Active));
        Assert.That(match.ActiveParticipantIndex, Is.EqualTo(0));
        Assert.That(match.Participants[0].Total, Is.EqualTo(8));
    }

    [Test]
    public void FirstDraw_AlternatesBetweenSets()
    {
        var match = CreateMatch();
        SetBoard(match.Participants[0], 20);
        SetBoard(match.Participants[1], 19);
        match.Participants[1].IsStanding = true;

        PazaakGameEngine.Stand(match, PazaakParticipantSlot.PlayerOne, new FixedRandom());

        Assert.That(match.CurrentSet, Is.EqualTo(2));
        Assert.That(match.ActiveParticipantIndex, Is.EqualTo(1));
    }

    [Test]
    public void CreateMatch_UsesExplicitFirstParticipantWhenProvided()
    {
        var match = PazaakGameEngine.CreateMatch(
            "one",
            "One",
            PazaakCardCatalog.StarterDeck,
            "two",
            "Two",
            PazaakCardCatalog.StarterDeck,
            true,
            false,
            false,
            0,
            new FixedRandom(),
            1);

        Assert.That(match.ActiveParticipantIndex, Is.EqualTo(1));
    }

    [Test]
    public void Ai_UsesSideCardToRecoverFromBust()
    {
        var match = CreateMatch();
        match.ActiveParticipantIndex = 1;
        SetBoard(match.Participants[1], 10, 9, 3);
        match.Participants[1].SideHand = new List<PazaakCardType> { PazaakCardType.Minus4 };

        var decision = PazaakAi.ChooseMove(match, PazaakParticipantSlot.PlayerTwo, PazaakNpcDifficulty.Expert);

        Assert.That(decision.ShouldPlaySideCard, Is.True);
        Assert.That(decision.SideHandIndex, Is.EqualTo(0));
    }

    private static PazaakMatchState CreateMatch()
    {
        return PazaakGameEngine.CreateMatch(
            "one",
            "One",
            PazaakCardCatalog.StarterDeck,
            "two",
            "Two",
            PazaakCardCatalog.StarterDeck,
            false,
            true,
            false,
            0,
            new FixedRandom());
    }

    private static void SetBoard(PazaakParticipantState participant, params int[] values)
    {
        participant.Board.Clear();
        foreach (var value in values)
        {
            participant.Board.Add(new PazaakPlayedCard(PazaakCardType.Invalid, value.ToString(), value, true));
        }
    }
}
