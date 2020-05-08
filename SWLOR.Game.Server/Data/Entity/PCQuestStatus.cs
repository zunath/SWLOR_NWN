
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCQuestStatus]")]
    public class PCQuestStatus: IEntity
    {
        public PCQuestStatus()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int QuestID { get; set; }
        public int QuestState { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int TimesCompleted { get; set; }

        public IEntity Clone()
        {
            return new PCQuestStatus
            {
                ID = ID,
                PlayerID = PlayerID,
                QuestID = QuestID,
                QuestState = QuestState,
                CompletionDate = CompletionDate,
                TimesCompleted = TimesCompleted
            };
        }
    }
}
