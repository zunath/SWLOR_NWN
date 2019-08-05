
using System;
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
        public int CurrentQuestStateID { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int? SelectedItemRewardID { get; set; }
        public int TimesCompleted { get; set; }

        public IEntity Clone()
        {
            return new PCQuestStatus
            {
                ID = ID,
                PlayerID = PlayerID,
                QuestID = QuestID,
                CurrentQuestStateID = CurrentQuestStateID,
                CompletionDate = CompletionDate,
                SelectedItemRewardID = SelectedItemRewardID,
                TimesCompleted = TimesCompleted
            };
        }
    }
}
