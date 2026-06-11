using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakParticipantState
    {
        public string ParticipantId { get; set; }
        public string Name { get; set; }
        public bool IsNpc { get; set; }
        public List<PazaakCardType> SideDeck { get; set; }
        public List<PazaakCardType> SideHand { get; set; }
        public List<PazaakPlayedCard> Board { get; set; }
        public int SetsWon { get; set; }
        public bool IsStanding { get; set; }
        public bool HasPlayedSideCardThisTurn { get; set; }

        public PazaakParticipantState()
        {
            ParticipantId = string.Empty;
            Name = string.Empty;
            SideDeck = new List<PazaakCardType>();
            SideHand = new List<PazaakCardType>();
            Board = new List<PazaakPlayedCard>();
        }

        public int Total => Board.Sum(x => x.Value);
        public int CardCount => Board.Count;
        public bool IsBusted => Total > PazaakGameEngine.TargetTotal;
        public bool HasTieBreaker => Board.Any(x => x.CardType == PazaakCardType.TieBreaker);
    }
}
