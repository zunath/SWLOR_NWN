using System;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnPlayerGuildRankUp
    {
        public Guid PlayerID { get; set; }
        public int Rank { get; set; }

        public OnPlayerGuildRankUp(Guid playerID, int rank)
        {
            PlayerID = playerID;
            Rank = rank;
        }
    }
}
