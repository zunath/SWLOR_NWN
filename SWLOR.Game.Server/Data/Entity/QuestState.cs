using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestState]")]
    public class QuestState: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int QuestID { get; set; }
        public int Sequence { get; set; }
        public int QuestTypeID { get; set; }
        public int JournalStateID { get; set; }

        public IEntity Clone()
        {
            return new QuestState
            {
                ID = ID,
                QuestID = QuestID,
                Sequence = Sequence,
                QuestTypeID = QuestTypeID,
                JournalStateID = JournalStateID
            };
        }
    }
}
