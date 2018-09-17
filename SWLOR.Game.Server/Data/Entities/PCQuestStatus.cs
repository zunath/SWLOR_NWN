using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCQuestStatus
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCQuestStatus()
        {
            PCQuestKillTargetProgresses = new HashSet<PCQuestKillTargetProgress>();
            PCQuestItemProgresses = new HashSet<PCQuestItemProgress>();
        }

        [Key]
        public int PCQuestStatusID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int QuestID { get; set; }

        public int CurrentQuestStateID { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? CompletionDate { get; set; }

        public int? SelectedItemRewardID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestItemProgress> PCQuestItemProgresses { get; set; }

        public virtual QuestState CurrentQuestState { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }

        public virtual Quest Quest { get; set; }

        public virtual QuestRewardItem QuestRewardItem { get; set; }
    }
}
