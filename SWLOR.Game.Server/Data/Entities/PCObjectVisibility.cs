using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("PCObjectVisibility")]
    public partial class PCObjectVisibility
    {
        public int PCObjectVisibilityID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        [Required]
        [StringLength(60)]
        public string VisibilityObjectID { get; set; }

        public bool IsVisible { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
