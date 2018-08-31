using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("PCCorpses")]
    public partial class PCCorpse
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCCorpse()
        {
            PCCorpseItems = new HashSet<PCCorpseItem>();
        }

        [Key]
        public long PCCorpseID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public double PositionX { get; set; }

        public double PositionY { get; set; }

        public double PositionZ { get; set; }

        public double Orientation { get; set; }

        [StringLength(32)]
        public string AreaTag { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCCorpseItem> PCCorpseItems { get; set; }
    }
}
