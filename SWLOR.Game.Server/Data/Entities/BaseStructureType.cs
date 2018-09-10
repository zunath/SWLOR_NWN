using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("BaseStructureType")]
    public partial class BaseStructureType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BaseStructureType()
        {
            BaseStructures = new HashSet<BaseStructure>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BaseStructureTypeID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        public bool IsActive { get; set; }

        public bool CanPlaceInside { get; set; }

        public bool CanPlaceOutside { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BaseStructure> BaseStructures { get; set; }
    }
}
