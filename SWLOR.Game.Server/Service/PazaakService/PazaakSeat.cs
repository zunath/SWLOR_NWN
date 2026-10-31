using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakSeat
    {
        public PazaakSeat(uint player, IEnumerable<PazaakCardType> sideDeck)
        {
            Player = player;
            SideDeck = sideDeck.ToList();
        }

        public uint Player { get; }
        public List<PazaakCardType> SideDeck { get; }

        /// <summary>
        /// Side cards dealt at the start of the match. Played cards are gone for the rest of the match.
        /// </summary>
        public List<PazaakCardType> Hand { get; } = new();

        public List<PazaakTableCard> Table { get; } = new();
        public List<int> MainDeck { get; } = new();
        public bool IsStanding { get; set; }
        public bool HasPlayedSideCardThisTurn { get; set; }
        public int SetsWon { get; set; }

        public int Total => Table.Sum(x => x.Value);

        public bool HasTiebreaker => Table.Any(x => x.SideCard == PazaakCardType.Tiebreaker);
    }
}
