
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("NPCGroups")]
    public class NPCGroup: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public NPCGroup()
        {
        }

        [ExplicitKey]
        public int NPCGroupID { get; set; }
        public string Name { get; set; }
    }
}
