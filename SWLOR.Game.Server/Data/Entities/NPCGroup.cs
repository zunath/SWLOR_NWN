using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class NPCGroup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public NPCGroup()
        {
            PCQuestKillTargetProgresses = new HashSet<PCQuestKillTargetProgress>();
            QuestKillTargetLists = new HashSet<QuestKillTargetList>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int NPCGroupID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QuestKillTargetList> QuestKillTargetLists { get; set; }
    }
}
