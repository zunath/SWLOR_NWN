using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("QuestTypeDomain")]
    public partial class QuestTypeDomain
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public QuestTypeDomain()
        {
            QuestStates = new HashSet<QuestState>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int QuestTypeID { get; set; }

        [Required]
        [StringLength(30)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QuestState> QuestStates { get; set; }
    }
}
