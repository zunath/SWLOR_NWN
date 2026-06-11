using System;

namespace SWLOR.Game.Server.Entity
{
    public class PazaakEscrow : EntityBase
    {
        [Indexed]
        public string MatchId { get; set; }

        [Indexed]
        public string PlayerOneId { get; set; }

        [Indexed]
        public string PlayerTwoId { get; set; }

        [Indexed]
        public bool IsSettled { get; set; }

        public int PlayerOneAmount { get; set; }
        public int PlayerTwoAmount { get; set; }
        public bool IsRated { get; set; }
        public bool IsPvP { get; set; }
        public DateTime DateSettled { get; set; }

        public PazaakEscrow()
        {
            MatchId = string.Empty;
            PlayerOneId = string.Empty;
            PlayerTwoId = string.Empty;
        }

        public PazaakEscrow(string matchId) : this()
        {
            Id = matchId;
            MatchId = matchId;
        }
    }
}
