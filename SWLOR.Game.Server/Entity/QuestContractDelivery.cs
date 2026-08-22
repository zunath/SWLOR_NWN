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
    }
}
