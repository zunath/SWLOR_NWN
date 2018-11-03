
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("QuestStates")]
    public class QuestState: IEntity
    {
        public QuestState()
        {
            QuestKillTargetLists = new HashSet<QuestKillTargetList>();
            QuestRequiredItemLists = new HashSet<QuestRequiredItemList>();
            QuestRequiredKeyItemLists = new HashSet<QuestRequiredKeyItemList>();
        }

        [Key]
        public int QuestStateID { get; set; }
        public int QuestID { get; set; }
        public int Sequence { get; set; }
        public int QuestTypeID { get; set; }
        public int JournalStateID { get; set; }
    
        public virtual ICollection<QuestKillTargetList> QuestKillTargetLists { get; set; }
        public virtual ICollection<QuestRequiredItemList> QuestRequiredItemLists { get; set; }
        public virtual ICollection<QuestRequiredKeyItemList> QuestRequiredKeyItemLists { get; set; }
    }
}
