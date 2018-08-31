using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class BaseItemType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BaseItemType()
        {
            PCMigrationItems = new HashSet<PCMigrationItem>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BaseItemTypeID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCMigrationItem> PCMigrationItems { get; set; }
    }
}
