using System.Collections.Generic;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class QuestState
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public QuestState()
        {
            PCQuestStatus = new HashSet<PCQuestStatus>();
            QuestKillTargetLists = new HashSet<QuestKillTargetList>();
            QuestRequiredItemLists = new HashSet<QuestRequiredItemList>();
            QuestRequiredKeyItemLists = new HashSet<QuestRequiredKeyItemList>();
        }

        public int QuestStateID { get; set; }

        public int QuestID { get; set; }

        public int Sequence { get; set; }

        public int QuestTypeID { get; set; }

        public int JournalStateID { get; set; }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestStatus> PCQuestStatus { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QuestKillTargetList> QuestKillTargetLists { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QuestRequiredItemList> QuestRequiredItemLists { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QuestRequiredKeyItemList> QuestRequiredKeyItemLists { get; set; }

        public virtual Quest Quest { get; set; }

        public virtual QuestTypeDomain QuestTypeDomain { get; set; }
    }
}
