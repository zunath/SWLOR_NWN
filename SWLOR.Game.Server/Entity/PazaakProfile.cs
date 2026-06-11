using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Entity
{
    public class PazaakProfile : EntityBase
    {
        [Indexed]
        public string PlayerId { get; set; }

        [Indexed]
        public int PvPRating { get; set; }

        public Dictionary<int, int> Collection { get; set; }
        public List<PazaakCardType> ActiveSideDeck { get; set; }
        public int PvPWins { get; set; }
        public int PvPLosses { get; set; }
        public int NPCWins { get; set; }
        public int NPCLosses { get; set; }
        public Dictionary<string, int> NamedNPCWins { get; set; }
        public HashSet<string> ClaimedNpcRewards { get; set; }
        public int PendingCreditPayout { get; set; }
        public DateTime DateUpdated { get; set; }

        public PazaakProfile()
        {
            PlayerId = string.Empty;
            PvPRating = Pazaak.InitialRating;
            Collection = new Dictionary<int, int>();
            ActiveSideDeck = new List<PazaakCardType>();
            NamedNPCWins = new Dictionary<string, int>();
            ClaimedNpcRewards = new HashSet<string>();
            DateUpdated = DateTime.UtcNow;
        }

        public PazaakProfile(string playerId) : this()
        {
            Id = playerId;
            PlayerId = playerId;
        }
    }
}
