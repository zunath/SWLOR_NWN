
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCQuestStatus: IEntity
    {
        public PCQuestStatus()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int QuestID { get; set; }
        public int QuestState { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int TimesCompleted { get; set; }
    }
}
