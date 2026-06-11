using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakNpcProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PazaakNpcDifficulty Difficulty { get; set; }
        public int MinimumWager { get; set; }
        public int MaximumWager { get; set; }
        public List<PazaakCardType> SideDeck { get; set; }
        public int RewardWinCount { get; set; }
        public PazaakCardType RewardCard { get; set; }
        public int RewardCardCount { get; set; }

        public PazaakNpcProfile()
        {
            Id = string.Empty;
            Name = string.Empty;
            SideDeck = new List<PazaakCardType>();
            RewardWinCount = 0;
            RewardCard = PazaakCardType.Invalid;
            RewardCardCount = 1;
        }
    }
}
