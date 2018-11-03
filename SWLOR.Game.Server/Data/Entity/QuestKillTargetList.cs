
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("QuestKillTargetList")]
    public partial class QuestKillTargetList: IEntity
    {
        [Key]
        public int QuestKillTargetListID { get; set; }
        public int QuestID { get; set; }
        public int NPCGroupID { get; set; }
        public int Quantity { get; set; }
        public int QuestStateID { get; set; }
    
        public virtual NPCGroup NPCGroup { get; set; }
        public virtual Quest Quest { get; set; }
        public virtual QuestState QuestState { get; set; }
    }
}
