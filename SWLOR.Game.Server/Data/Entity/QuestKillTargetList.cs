

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestKillTargetList]")]
    public class QuestKillTargetList: IEntity
    {
        [Key]
        public int QuestKillTargetListID { get; set; }
        public int QuestID { get; set; }
        public int NPCGroupID { get; set; }
        public int Quantity { get; set; }
        public int QuestStateID { get; set; }
    }
}
