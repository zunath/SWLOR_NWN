using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class QuestRewardItem
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public QuestRewardItem()
        {
            PCQuestStatus = new HashSet<PCQuestStatus>();
        }

        public int QuestRewardItemID { get; set; }

        public int QuestID { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        public int Quantity { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestStatus> PCQuestStatus { get; set; }

        public virtual Quest Quest { get; set; }
    }
}
