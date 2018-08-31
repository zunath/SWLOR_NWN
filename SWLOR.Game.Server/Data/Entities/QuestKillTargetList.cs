using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("QuestKillTargetList")]
    public partial class QuestKillTargetList
    {
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
