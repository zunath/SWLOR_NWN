using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    public class QuestContractItem
    {
        public QuestContractItem()
        {
            Data = string.Empty;
            Name = string.Empty;
            Resref = string.Empty;
            IconResref = string.Empty;
        }

        public string Data { get; set; }
        public string Name { get; set; }
        public string Resref { get; set; }
        public int StackSize { get; set; }
        public string IconResref { get; set; }

        /// <summary>
        /// Credits for the retired configuration modules the space revamp's migration took out of this item (a bag),
        /// paid to whoever claims it.
        /// </summary>
        public int RetiredSpaceCredits { get; set; }

        /// <summary>
        /// Hull Designs the space revamp's migration owes with this item (a converted deed keeps its look as one),
        /// unlocked for whoever claims it. Null when none.
        /// </summary>
        public List<string> HullDesigns { get; set; }
    }
}
