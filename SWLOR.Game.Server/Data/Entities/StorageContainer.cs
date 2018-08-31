using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class StorageContainer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StorageContainer()
        {
            StorageItems = new HashSet<StorageItem>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int StorageContainerID { get; set; }

        [Required]
        [StringLength(255)]
        public string AreaName { get; set; }

        [Required]
        [StringLength(64)]
        public string AreaTag { get; set; }

        [Required]
        [StringLength(16)]
        public string AreaResref { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StorageItem> StorageItems { get; set; }
    }
}
