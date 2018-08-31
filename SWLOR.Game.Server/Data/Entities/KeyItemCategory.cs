using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class KeyItemCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public KeyItemCategory()
        {
            KeyItems = new HashSet<KeyItem>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int KeyItemCategoryID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        public bool IsActive { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<KeyItem> KeyItems { get; set; }
    }
}
