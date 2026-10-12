using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Entity
{
    public class QuestContractDelivery: EntityBase
    {
        public QuestContractDelivery()
        {
            PlayerId = string.Empty;
            Items = new List<QuestContractItem>();
            SourceContractId = string.Empty;
            SourceContractTitle = string.Empty;
        }

        [Indexed]
        public string PlayerId { get; set; }
        public int Credits { get; set; }
        public List<QuestContractItem> Items { get; set; }
        [Indexed]
        public string SourceContractId { get; set; }
        public string SourceContractTitle { get; set; }
        // Objective submissions remain owned by their submitter until that attempt wins.
        public bool HeldForCompletion { get; set; }
        [Indexed]
        public int HeldSubmissionIndex => HeldForCompletion ? 1 : 0;
        // Keep an empty receipt after claiming so interrupted settlement cannot pay twice.
        public bool IsRewardPayment { get; set; }
        public int ClaimRevision { get; set; }
    }
}
