
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("QuestStates")]
    public partial class QuestState: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public QuestState()
        {
            this.PCQuestStatus = new HashSet<PCQuestStatus>();
            this.QuestKillTargetLists = new HashSet<QuestKillTargetList>();
            this.QuestRequiredItemLists = new HashSet<QuestRequiredItemList>();
            this.QuestRequiredKeyItemLists = new HashSet<QuestRequiredKeyItemList>();
        }

        [Key]
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
