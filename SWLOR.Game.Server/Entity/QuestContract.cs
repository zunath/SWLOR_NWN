using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Entity
{
    public class QuestContract: EntityBase
    {
        public QuestContract()
        {
            AuthorPlayerId = string.Empty;
            AuthorCDKey = string.Empty;
            AuthorName = string.Empty;
            Title = string.Empty;
            Description = string.Empty;
            Objectives = new List<QuestContractObjective>();
            RewardItems = new List<QuestContractItem>();
            TakedownPlayerId = string.Empty;
            TakedownReason = string.Empty;
        }

        [Indexed]
        public string AuthorPlayerId { get; set; }
        [Indexed]
        public string AuthorCDKey { get; set; }
        [Indexed]
        public QuestContractStatus Status { get; set; }
        public string AuthorName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<QuestContractObjective> Objectives { get; set; }
        public int RewardCredits { get; set; }
        public List<QuestContractItem> RewardItems { get; set; }
        public int CompletionsRemaining { get; set; }
        public DateTime DatePublished { get; set; }
        public DateTime DateExpires { get; set; }
        public string TakedownPlayerId { get; set; }
        public string TakedownReason { get; set; }
    }
}
