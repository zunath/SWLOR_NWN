using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakMatchState
    {
        public string MatchId { get; set; }
        public PazaakMatchStatus Status { get; set; }
        public bool IsRated { get; set; }
        public bool IsPvP { get; set; }
        public int Wager { get; set; }
        public int CurrentSet { get; set; }
        public int ActiveParticipantIndex { get; set; }
        public int CurrentSetFirstParticipantIndex { get; set; }
        public int WinnerIndex { get; set; }
        public string StatusText { get; set; }
        public List<int> MainDeck { get; set; }
        public PazaakParticipantState[] Participants { get; set; }

        public PazaakMatchState()
        {
            MatchId = Guid.NewGuid().ToString();
            Status = PazaakMatchStatus.Active;
            WinnerIndex = -1;
            StatusText = string.Empty;
            MainDeck = new List<int>();
            Participants = new[]
            {
                new PazaakParticipantState(),
                new PazaakParticipantState(),
            };
        }
    }
}
