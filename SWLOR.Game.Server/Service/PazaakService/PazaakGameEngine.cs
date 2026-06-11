using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public static class PazaakGameEngine
    {
        public const int TargetTotal = 20;
        public const int RequiredSideDeckSize = 10;
        public const int MatchWinningSetCount = 3;
        public const int SideHandSize = 4;
        public const int MaxBoardCardCount = 9;

        public static string ValidateSideDeck(IEnumerable<PazaakCardType> sideDeck)
        {
            if (sideDeck == null)
                return "A side deck is required.";

            var cards = sideDeck.ToList();
            if (cards.Count != RequiredSideDeckSize)
                return $"A side deck must contain exactly {RequiredSideDeckSize} cards.";

            foreach (var card in cards)
            {
                if (!PazaakCardCatalog.IsValidCard(card))
                    return $"Invalid Pazaak card: {(int)card}.";
            }

            return string.Empty;
        }

        public static PazaakMatchState CreateMatch(
            string playerOneId,
            string playerOneName,
            IEnumerable<PazaakCardType> playerOneSideDeck,
            string playerTwoId,
            string playerTwoName,
            IEnumerable<PazaakCardType> playerTwoSideDeck,
            bool isPlayerTwoNpc,
            bool isPvP,
            bool isRated,
            int wager,
            IPazaakRandom random,
            int? firstParticipantIndex = null)
        {
            var playerOneDeck = playerOneSideDeck.ToList();
            var playerTwoDeck = playerTwoSideDeck.ToList();
            var playerOneValidation = ValidateSideDeck(playerOneDeck);
            if (!string.IsNullOrWhiteSpace(playerOneValidation))
                throw new ArgumentException(playerOneValidation, nameof(playerOneSideDeck));

            var playerTwoValidation = ValidateSideDeck(playerTwoDeck);
            if (!string.IsNullOrWhiteSpace(playerTwoValidation))
                throw new ArgumentException(playerTwoValidation, nameof(playerTwoSideDeck));

            var openingParticipantIndex = firstParticipantIndex.HasValue
                ? Math.Clamp(firstParticipantIndex.Value, 0, 1)
                : random.Next(2);

            var match = new PazaakMatchState
            {
                IsPvP = isPvP,
                IsRated = isRated,
                Wager = wager,
                CurrentSetFirstParticipantIndex = openingParticipantIndex,
            };
            match.Participants[0] = new PazaakParticipantState
            {
                ParticipantId = playerOneId,
                Name = playerOneName,
                IsNpc = false,
                SideDeck = playerOneDeck,
                SideHand = DrawSideHand(playerOneDeck, random),
            };
            match.Participants[1] = new PazaakParticipantState
            {
                ParticipantId = playerTwoId,
                Name = playerTwoName,
                IsNpc = isPlayerTwoNpc,
                SideDeck = playerTwoDeck,
                SideHand = DrawSideHand(playerTwoDeck, random),
            };

            StartNextSet(match, random);
            return match;
        }

        public static void PlaySideCard(
            PazaakMatchState match,
            PazaakParticipantSlot slot,
            int sideHandIndex,
            int selectedValue,
            IPazaakRandom random)
        {
            if (!CanAct(match, slot))
                return;

            var participant = GetParticipant(match, slot);
            if (participant.HasPlayedSideCardThisTurn)
                throw new InvalidOperationException("Only one side card can be played per turn.");

            if (sideHandIndex < 0 || sideHandIndex >= participant.SideHand.Count)
                throw new ArgumentOutOfRangeException(nameof(sideHandIndex));

            var cardType = participant.SideHand[sideHandIndex];
            var card = PazaakCardCatalog.Get(cardType);
            PazaakPlayedCard playedCard;

            switch (card.Rule)
            {
                case PazaakCardRule.FixedValue:
                    playedCard = new PazaakPlayedCard(card.Type, card.ShortName, card.PlayableValues[0], false);
                    break;
                case PazaakCardRule.ChooseValue:
                    if (!card.PlayableValues.Contains(selectedValue))
                        throw new ArgumentException("Selected value is not valid for this card.", nameof(selectedValue));

                    playedCard = new PazaakPlayedCard(card.Type, FormatSigned(selectedValue), selectedValue, false);
                    break;
                case PazaakCardRule.DoubleLastCard:
                    if (participant.Board.Count <= 0)
                        throw new InvalidOperationException("Double requires a card already on the board.");

                    var doubledValue = participant.Board[participant.Board.Count - 1].Value;
                    playedCard = new PazaakPlayedCard(card.Type, $"{card.ShortName} {FormatSigned(doubledValue)}", doubledValue, false);
                    break;
                case PazaakCardRule.TieBreaker:
                    if (!card.PlayableValues.Contains(selectedValue))
                        throw new ArgumentException("Selected value is not valid for this card.", nameof(selectedValue));

                    playedCard = new PazaakPlayedCard(card.Type, $"{card.ShortName} {FormatSigned(selectedValue)}", selectedValue, false);
                    break;
                case PazaakCardRule.FlipValues:
                    foreach (var boardCard in participant.Board)
                    {
                        if (boardCard.Value > 0 &&
                            card.FlipValues.Contains(boardCard.Value))
                        {
                            boardCard.Value *= -1;
                            boardCard.Label = FormatSigned(boardCard.Value);
                        }
                    }

                    playedCard = new PazaakPlayedCard(card.Type, card.ShortName, 0, false);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported Pazaak card rule {card.Rule}.");
            }

            participant.SideHand.RemoveAt(sideHandIndex);
            participant.HasPlayedSideCardThisTurn = true;
            participant.Board.Add(playedCard);

            if (card.Rule == PazaakCardRule.TieBreaker &&
                match.Participants[OpponentOf((int)slot)].IsStanding &&
                participant.Total <= TargetTotal &&
                participant.Total == match.Participants[OpponentOf((int)slot)].Total)
            {
                CompleteSet(match, (int)slot, random, $"{participant.Name} wins the tied set with Tie Breaker.");
                return;
            }

            EvaluateImmediateCardCountWin(match, (int)slot, random);
        }

        public static void EndTurn(PazaakMatchState match, PazaakParticipantSlot slot, IPazaakRandom random)
        {
            if (!CanAct(match, slot))
                return;

            var participantIndex = (int)slot;
            var participant = match.Participants[participantIndex];
            if (participant.Total > TargetTotal)
            {
                CompleteSet(match, OpponentOf(participantIndex), random, $"{participant.Name} busts.");
                return;
            }

            if (participant.CardCount >= MaxBoardCardCount)
            {
                CompleteSet(match, participantIndex, random, $"{participant.Name} wins with nine cards.");
                return;
            }

            participant.HasPlayedSideCardThisTurn = false;
            AdvanceTurn(match, random);
        }

        public static void Stand(PazaakMatchState match, PazaakParticipantSlot slot, IPazaakRandom random)
        {
            if (!CanAct(match, slot))
                return;

            var participantIndex = (int)slot;
            var participant = match.Participants[participantIndex];
            if (participant.Total > TargetTotal)
            {
                CompleteSet(match, OpponentOf(participantIndex), random, $"{participant.Name} busts while standing.");
                return;
            }

            if (participant.CardCount >= MaxBoardCardCount)
            {
                CompleteSet(match, participantIndex, random, $"{participant.Name} wins with nine cards.");
                return;
            }

            participant.IsStanding = true;
            participant.HasPlayedSideCardThisTurn = false;

            if (match.Participants[OpponentOf(participantIndex)].IsStanding)
            {
                ResolveStandingSet(match, random);
                return;
            }

            AdvanceTurn(match, random);
        }

        public static void Forfeit(PazaakMatchState match, PazaakParticipantSlot slot)
        {
            if (match.Status != PazaakMatchStatus.Active)
                return;

            var winner = OpponentOf((int)slot);
            match.Status = PazaakMatchStatus.Forfeit;
            match.WinnerIndex = winner;
            match.StatusText = $"{match.Participants[(int)slot].Name} forfeits. {match.Participants[winner].Name} wins.";
        }

        public static PazaakParticipantState GetParticipant(PazaakMatchState match, PazaakParticipantSlot slot)
        {
            return match.Participants[(int)slot];
        }

        public static PazaakParticipantSlot GetOpponent(PazaakParticipantSlot slot)
        {
            return (PazaakParticipantSlot)OpponentOf((int)slot);
        }

        private static bool CanAct(PazaakMatchState match, PazaakParticipantSlot slot)
        {
            return match.Status == PazaakMatchStatus.Active &&
                   match.ActiveParticipantIndex == (int)slot &&
                   !match.Participants[(int)slot].IsStanding;
        }

        private static void StartNextSet(PazaakMatchState match, IPazaakRandom random, string previousSetResult = null)
        {
            match.CurrentSet++;
            match.MainDeck = BuildMainDeck(random);
            match.ActiveParticipantIndex = match.CurrentSetFirstParticipantIndex;
            match.WinnerIndex = -1;

            foreach (var participant in match.Participants)
            {
                participant.Board.Clear();
                participant.IsStanding = false;
                participant.HasPlayedSideCardThisTurn = false;
            }

            DrawMainCard(match, match.ActiveParticipantIndex, random);
            var openerText = $"Set {match.CurrentSet}: {match.Participants[match.ActiveParticipantIndex].Name} draws first.";
            match.StatusText = string.IsNullOrWhiteSpace(previousSetResult)
                ? openerText
                : $"{previousSetResult} {openerText}";
        }

        private static List<int> BuildMainDeck(IPazaakRandom random)
        {
            var cards = new List<int>();
            for (var value = 1; value <= 10; value++)
            {
                for (var count = 0; count < 4; count++)
                {
                    cards.Add(value);
                }
            }

            Shuffle(cards, random);
            return cards;
        }

        private static List<PazaakCardType> DrawSideHand(IReadOnlyList<PazaakCardType> sideDeck, IPazaakRandom random)
        {
            var cards = sideDeck.ToList();
            Shuffle(cards, random);
            return cards.Take(SideHandSize).ToList();
        }

        private static void Shuffle<T>(IList<T> cards, IPazaakRandom random)
        {
            for (var index = cards.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
            }
        }

        private static void DrawMainCard(PazaakMatchState match, int participantIndex, IPazaakRandom random)
        {
            if (match.MainDeck.Count <= 0)
            {
                match.MainDeck = BuildMainDeck(random);
            }

            var value = match.MainDeck[0];
            match.MainDeck.RemoveAt(0);
            match.Participants[participantIndex].Board.Add(
                new PazaakPlayedCard(PazaakCardType.Invalid, value.ToString(), value, true));
        }

        private static void AdvanceTurn(PazaakMatchState match, IPazaakRandom random)
        {
            var current = match.ActiveParticipantIndex;
            var next = OpponentOf(current);
            if (match.Participants[next].IsStanding)
            {
                if (match.Participants[current].IsStanding)
                {
                    ResolveStandingSet(match, random);
                }
                else
                {
                    DrawMainCard(match, current, random);
                    EvaluateImmediateCardCountWin(match, current, random);
                }

                return;
            }

            match.ActiveParticipantIndex = next;
            DrawMainCard(match, next, random);
            EvaluateImmediateCardCountWin(match, next, random);
        }

        private static void EvaluateImmediateCardCountWin(PazaakMatchState match, int participantIndex, IPazaakRandom random)
        {
            var participant = match.Participants[participantIndex];
            if (match.Status == PazaakMatchStatus.Active &&
                participant.CardCount >= MaxBoardCardCount &&
                participant.Total <= TargetTotal)
            {
                CompleteSet(match, participantIndex, random, $"{participant.Name} wins with nine cards.");
            }
        }

        private static void ResolveStandingSet(PazaakMatchState match, IPazaakRandom random)
        {
            var one = match.Participants[0];
            var two = match.Participants[1];

            var oneTotal = one.Total > TargetTotal ? -1 : one.Total;
            var twoTotal = two.Total > TargetTotal ? -1 : two.Total;

            if (oneTotal > twoTotal)
            {
                CompleteSet(match, 0, random, $"{one.Name} wins the set.");
            }
            else if (twoTotal > oneTotal)
            {
                CompleteSet(match, 1, random, $"{two.Name} wins the set.");
            }
            else if (one.HasTieBreaker && !two.HasTieBreaker)
            {
                CompleteSet(match, 0, random, $"{one.Name} wins the tied set with Tie Breaker.");
            }
            else if (two.HasTieBreaker && !one.HasTieBreaker)
            {
                CompleteSet(match, 1, random, $"{two.Name} wins the tied set with Tie Breaker.");
            }
            else
            {
                match.CurrentSetFirstParticipantIndex = OpponentOf(match.CurrentSetFirstParticipantIndex);
                StartNextSet(match, random, "The set is tied. A new set begins.");
            }
        }

        private static void CompleteSet(PazaakMatchState match, int winnerIndex, IPazaakRandom random, string reason)
        {
            match.Participants[winnerIndex].SetsWon++;
            match.StatusText = reason;

            if (match.Participants[winnerIndex].SetsWon >= MatchWinningSetCount)
            {
                match.Status = PazaakMatchStatus.Complete;
                match.WinnerIndex = winnerIndex;
                match.StatusText = $"{reason} {match.Participants[winnerIndex].Name} wins the match.";
                return;
            }

            match.CurrentSetFirstParticipantIndex = OpponentOf(match.CurrentSetFirstParticipantIndex);
            StartNextSet(match, random, reason);
        }

        private static int OpponentOf(int participantIndex)
        {
            return participantIndex == 0 ? 1 : 0;
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }
}
