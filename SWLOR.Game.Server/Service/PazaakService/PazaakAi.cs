using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public static class PazaakAi
    {
        public static PazaakAiDecision ChooseMove(
            PazaakMatchState match,
            PazaakParticipantSlot slot,
            PazaakNpcDifficulty difficulty)
        {
            var participant = PazaakGameEngine.GetParticipant(match, slot);
            var opponent = PazaakGameEngine.GetParticipant(match, PazaakGameEngine.GetOpponent(slot));
            var targetStandTotal = difficulty switch
            {
                PazaakNpcDifficulty.Novice => 18,
                PazaakNpcDifficulty.Skilled => 19,
                PazaakNpcDifficulty.Expert => 19,
                _ => 20,
            };

            if (!participant.HasPlayedSideCardThisTurn)
            {
                var cardPlay = FindBestSideCard(participant, opponent, difficulty);
                if (cardPlay.SideHandIndex >= 0)
                    return cardPlay;
            }

            var total = participant.Total;
            var shouldStand =
                total == PazaakGameEngine.TargetTotal ||
                total >= targetStandTotal && total >= opponent.Total ||
                opponent.IsStanding && total > opponent.Total && total <= PazaakGameEngine.TargetTotal;

            return new PazaakAiDecision
            {
                SideHandIndex = -1,
                ShouldStand = shouldStand,
            };
        }

        private static PazaakAiDecision FindBestSideCard(
            PazaakParticipantState participant,
            PazaakParticipantState opponent,
            PazaakNpcDifficulty difficulty)
        {
            var bestScore = ScoreTotal(participant.Total, opponent, difficulty);
            var bestDecision = PazaakAiDecision.EndTurn();

            for (var index = 0; index < participant.SideHand.Count; index++)
            {
                var cardType = participant.SideHand[index];
                var card = PazaakCardCatalog.Get(cardType);

                foreach (var option in GetPlayableOptions(participant, card))
                {
                    var simulatedTotal = SimulateTotal(participant, card, option);
                var score = ScoreTotal(simulatedTotal, opponent, difficulty);
                if (card.Rule == PazaakCardRule.TieBreaker &&
                    opponent.IsStanding &&
                    simulatedTotal == opponent.Total &&
                    simulatedTotal <= PazaakGameEngine.TargetTotal)
                {
                    score += 500;
                }

                if (score <= bestScore)
                    continue;

                    bestScore = score;
                    bestDecision = new PazaakAiDecision
                    {
                        ShouldPlaySideCard = true,
                        SideHandIndex = index,
                        SelectedValue = option,
                        ShouldStand = simulatedTotal >= 19 && simulatedTotal <= PazaakGameEngine.TargetTotal,
                    };
                }
            }

            return bestDecision;
        }

        private static IEnumerable<int> GetPlayableOptions(PazaakParticipantState participant, PazaakCardDefinition card)
        {
            if (card.Rule == PazaakCardRule.DoubleLastCard && participant.Board.Count <= 0)
                return new int[0];

            if (card.Rule == PazaakCardRule.DoubleLastCard)
                return new[] { participant.Board[participant.Board.Count - 1].Value };

            if (card.Rule == PazaakCardRule.FlipValues)
                return new[] { 0 };

            return card.PlayableValues;
        }

        private static int SimulateTotal(PazaakParticipantState participant, PazaakCardDefinition card, int option)
        {
            if (card.Rule == PazaakCardRule.FlipValues)
            {
                var total = 0;
                foreach (var boardCard in participant.Board)
                {
                    var value = boardCard.Value;
                    if (value > 0 &&
                        card.FlipValues.Contains(value))
                    {
                        value *= -1;
                    }

                    total += value;
                }

                return total;
            }

            return participant.Total + option;
        }

        private static int ScoreTotal(
            int total,
            PazaakParticipantState opponent,
            PazaakNpcDifficulty difficulty)
        {
            if (total > PazaakGameEngine.TargetTotal)
                return -1000 - total;

            var score = total * 10;
            if (total == PazaakGameEngine.TargetTotal)
                score += 200;

            if (opponent.IsStanding && total > opponent.Total)
                score += 100;

            if (difficulty == PazaakNpcDifficulty.Novice && total < 18)
                score -= 20;
            else if ((int)difficulty >= (int)PazaakNpcDifficulty.Expert && total >= 19)
                score += 40;

            return score;
        }
    }
}
