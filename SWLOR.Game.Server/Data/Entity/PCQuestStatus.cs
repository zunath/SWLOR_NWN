
using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCQuestStatus")]
    public partial class PCQuestStatus: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCQuestStatus()
        {
            this.PCQuestItemProgresses = new HashSet<PCQuestItemProgress>();
            this.PCQuestKillTargetProgresses = new HashSet<PCQuestKillTargetProgress>();
        }

        [Key]
        public int PCQuestStatusID { get; set; }
        public string PlayerID { get; set; }
        public int QuestID { get; set; }
        public int CurrentQuestStateID { get; set; }
        public Nullable<System.DateTime> CompletionDate { get; set; }
        public Nullable<int> SelectedItemRewardID { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestItemProgress> PCQuestItemProgresses { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }
        public virtual QuestState CurrentQuestState { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
        public virtual Quest Quest { get; set; }
        public virtual QuestRewardItem SelectedQuestRewardItem { get; set; }
    }
}
