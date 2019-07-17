using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestKillTarget]")]
    public class QuestKillTarget: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int QuestID { get; set; }
        public int NPCGroupID { get; set; }
        public int Quantity { get; set; }
        public int QuestStateID { get; set; }

        public IEntity Clone()
        {
            return new QuestKillTarget
            {
                ID = ID,
                QuestID = QuestID,
                NPCGroupID = NPCGroupID,
                Quantity = Quantity,
                QuestStateID = QuestStateID
            };
        }
    }
}
