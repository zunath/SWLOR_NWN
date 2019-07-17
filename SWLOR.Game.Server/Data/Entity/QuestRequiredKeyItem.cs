using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestRequiredKeyItem]")]
    public class QuestRequiredKeyItem: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int QuestID { get; set; }
        public int KeyItemID { get; set; }
        public int QuestStateID { get; set; }

        public IEntity Clone()
        {
            return new QuestRequiredKeyItem
            {
                ID = ID,
                QuestID = QuestID,
                KeyItemID = KeyItemID,
                QuestStateID = QuestStateID
            };
        }
    }
}
