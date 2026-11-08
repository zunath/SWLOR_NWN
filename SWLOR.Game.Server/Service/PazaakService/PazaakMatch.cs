using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PazaakService
{
    /// <summary>
    /// Rules of one two-player Pazaak match. Holds no engine state so it can be tested directly.
    /// Each set, players take turns drawing from one shared main deck (four each of 1-10) onto their table and may play
    /// one side card per turn. A total above 20 when a turn ends is a bust. The higher total wins
    /// the set when both players stand, and filling all nine table slots without busting wins it
    /// outright. The first player to win three sets wins the match.
    /// </summary>
    public class PazaakMatch
    {
        public const int TargetTotal = 20;
        public const int TableSize = 9;
        public const int SetsToWin = 3;
        private const int MainDeckCopies = 4;
        private const int MainDeckHighestCard = 10;

        private readonly Func<int, int> _nextInt;
        private int _setStarter;

        /// <param name="nextInt">Returns a random number from 0 up to, but not including, the argument.</param>
        public PazaakMatch(PazaakSeat first, PazaakSeat second, Func<int, int> nextInt)
        {
            Seats = new[] { first, second };
            _nextInt = nextInt;
        }

        public PazaakSeat[] Seats { get; }

        /// <summary>
        /// The shared main deck both players draw from. The last card is drawn first.
        /// </summary>
        public List<int> MainDeck { get; } = new();

        public int CurrentSeat { get; private set; }
        public int SetNumber { get; private set; }

        /// <summary>
        /// Increases every time a turn begins, so a delayed timeout can tell whether its turn is still running.
        /// </summary>
        public int TurnNumber { get; private set; }

        public bool IsFinished { get; private set; }
        public int WinnerSeat { get; private set; } = -1;
        public bool WasForfeited { get; private set; }
        public PazaakSetResult LastSetResult { get; private set; }

        public static int GetOpponent(int seat) => 1 - seat;

        public void Start(int firstSeat)
        {
            foreach (var seat in Seats)
            {
                DealHand(seat);
            }

            StartSet(firstSeat);
        }

        public bool PlaySideCard(int seatIndex, int handIndex, bool negative, out string error)
        {
            if (!CanAct(seatIndex, out error))
                return false;

            var seat = Seats[seatIndex];
            if (seat.HasPlayedSideCardThisTurn)
            {
                error = "You have already played a side card this turn.";
                return false;
            }

            if (handIndex < 0 || handIndex >= seat.Hand.Count)
            {
                error = "That card is no longer in your hand.";
                return false;
            }

            if (seat.Table.Count >= TableSize)
            {
                error = "Your table is full.";
                return false;
            }

            var type = seat.Hand[handIndex];
            var detail = PazaakCards.Get(type);
            switch (detail.Kind)
            {
                case PazaakCardKind.Plus:
                    seat.Table.Add(new PazaakTableCard(detail.Value, type));
                    break;
                case PazaakCardKind.Minus:
                    seat.Table.Add(new PazaakTableCard(-detail.Value, type));
                    break;
                case PazaakCardKind.PlusMinus:
                case PazaakCardKind.Tiebreaker:
                    seat.Table.Add(new PazaakTableCard(negative ? -detail.Value : detail.Value, type));
                    break;
                case PazaakCardKind.FlipTwoFour:
                    FlipCards(seat, 2, 4);
                    seat.Table.Add(new PazaakTableCard(0, type));
                    break;
                case PazaakCardKind.FlipThreeSix:
                    FlipCards(seat, 3, 6);
                    seat.Table.Add(new PazaakTableCard(0, type));
                    break;
                case PazaakCardKind.Double:
                    // Adding the last card's value again doubles it on the table.
                    seat.Table.Add(new PazaakTableCard(seat.Table[^1].Value, type));
                    break;
            }

            seat.Hand.RemoveAt(handIndex);
            seat.HasPlayedSideCardThisTurn = true;

            if (seat.Table.Count >= TableSize && seat.Total <= TargetTotal)
                EndSet(seatIndex, wasBust: false, wasFullTable: true);

            error = string.Empty;
            return true;
        }

        public bool EndTurn(int seatIndex, out string error)
        {
            if (!CanAct(seatIndex, out error))
                return false;

            if (Seats[seatIndex].Total > TargetTotal)
            {
                EndSet(GetOpponent(seatIndex), wasBust: true, wasFullTable: false);
                return true;
            }

            AdvanceTurn();
            return true;
        }

        public bool Stand(int seatIndex, out string error)
        {
            if (!CanAct(seatIndex, out error))
                return false;

            if (Seats[seatIndex].Total > TargetTotal)
            {
                EndSet(GetOpponent(seatIndex), wasBust: true, wasFullTable: false);
                return true;
            }

            Seats[seatIndex].IsStanding = true;
            AdvanceTurn();
            return true;
        }

        public void Forfeit(int seatIndex)
        {
            if (IsFinished)
                return;

            IsFinished = true;
            WasForfeited = true;
            WinnerSeat = GetOpponent(seatIndex);
        }

        private bool CanAct(int seatIndex, out string error)
        {
            if (IsFinished)
            {
                error = "The match is over.";
                return false;
            }

            if (seatIndex != CurrentSeat)
            {
                error = "It is not your turn.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void DealHand(PazaakSeat seat)
        {
            var pool = new List<PazaakCardType>(seat.SideDeck);
            seat.Hand.Clear();
            for (var count = 0; count < PazaakCards.HandSize && pool.Count > 0; count++)
            {
                var index = _nextInt(pool.Count);
                seat.Hand.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }

        private void StartSet(int starter)
        {
            SetNumber++;
            _setStarter = starter;

            foreach (var seat in Seats)
            {
                seat.Table.Clear();
                seat.IsStanding = false;
            }

            ShuffleMainDeck();

            CurrentSeat = starter;
            BeginTurn();
        }

        private void ShuffleMainDeck()
        {
            MainDeck.Clear();
            for (var value = 1; value <= MainDeckHighestCard; value++)
            {
                for (var copy = 0; copy < MainDeckCopies; copy++)
                {
                    MainDeck.Add(value);
                }
            }

            for (var index = MainDeck.Count - 1; index > 0; index--)
            {
                var swap = _nextInt(index + 1);
                (MainDeck[index], MainDeck[swap]) = (MainDeck[swap], MainDeck[index]);
            }
        }

        private void BeginTurn()
        {
            TurnNumber++;
            var seat = Seats[CurrentSeat];
            seat.HasPlayedSideCardThisTurn = false;

            // A set uses at most 18 cards, so the 40-card deck never runs out before the next shuffle.
            var card = MainDeck[^1];
            MainDeck.RemoveAt(MainDeck.Count - 1);
            seat.Table.Add(new PazaakTableCard(card));

            if (seat.Table.Count >= TableSize && seat.Total <= TargetTotal)
                EndSet(CurrentSeat, wasBust: false, wasFullTable: true);
        }

        private void AdvanceTurn()
        {
            var opponent = GetOpponent(CurrentSeat);
            if (Seats[CurrentSeat].IsStanding && Seats[opponent].IsStanding)
            {
                ResolveSet();
                return;
            }

            // A player keeps drawing while the opponent stands.
            if (!Seats[opponent].IsStanding)
                CurrentSeat = opponent;

            BeginTurn();
        }

        private void ResolveSet()
        {
            var first = Seats[0];
            var second = Seats[1];
            int winner;

            if (first.Total > second.Total)
                winner = 0;
            else if (second.Total > first.Total)
                winner = 1;
            else if (first.HasTiebreaker != second.HasTiebreaker)
                winner = first.HasTiebreaker ? 0 : 1;
            else
                winner = -1;

            EndSet(winner, wasBust: false, wasFullTable: false);
        }

        private void EndSet(int winner, bool wasBust, bool wasFullTable)
        {
            LastSetResult = new PazaakSetResult(
                SetNumber,
                winner,
                new[] { Seats[0].Total, Seats[1].Total },
                wasBust,
                wasFullTable);

            if (winner >= 0)
            {
                Seats[winner].SetsWon++;
                if (Seats[winner].SetsWon >= SetsToWin)
                {
                    IsFinished = true;
                    WinnerSeat = winner;
                    return;
                }
            }

            // The loser of a set starts the next one. After a tie, the other player starts.
            var nextStarter = winner >= 0 ? GetOpponent(winner) : GetOpponent(_setStarter);
            StartSet(nextStarter);
        }

        private static void FlipCards(PazaakSeat seat, int low, int high)
        {
            foreach (var card in seat.Table)
            {
                var magnitude = Math.Abs(card.Value);
                if (magnitude == low || magnitude == high)
                    card.Value = -card.Value;
            }
        }
    }
}
