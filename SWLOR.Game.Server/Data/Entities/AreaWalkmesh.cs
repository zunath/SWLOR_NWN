using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("AreaWalkmesh")]
    public partial class AreaWalkmesh
    {
        public int AreaWalkmeshID { get; set; }

        [Required]
        [StringLength(60)]
        public string AreaID { get; set; }

        public double LocationX { get; set; }

        public double LocationY { get; set; }

        public double LocationZ { get; set; }
        
        public virtual Area Area { get; set; }
    }
}
