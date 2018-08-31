using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class ItemType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ItemType()
        {
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ItemTypeID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }
    }
}
