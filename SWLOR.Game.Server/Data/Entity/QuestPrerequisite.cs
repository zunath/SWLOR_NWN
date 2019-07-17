using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestPrerequisite]")]
    public class QuestPrerequisite: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int QuestID { get; set; }
        public int RequiredQuestID { get; set; }

        public IEntity Clone()
        {
            return new QuestPrerequisite
            {
                ID = ID,
                QuestID = QuestID,
                RequiredQuestID = RequiredQuestID
            };
        }
    }
}
