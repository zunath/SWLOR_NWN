
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("Mods")]
    public class Mod: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Mod()
        {
            Name = "";
            Script = "";
        }

        [ExplicitKey]
        public int ModID { get; set; }
        public string Name { get; set; }
        public string Script { get; set; }
        public bool IsActive { get; set; }
    }
}
