using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class Mod
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Mod()
        {
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ModID { get; set; }
        
        public string Name { get; set; }

        public string Script { get; set; }

        public bool IsActive { get; set; }

    }
}
