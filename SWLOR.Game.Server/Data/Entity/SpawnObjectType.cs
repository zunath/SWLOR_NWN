
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("SpawnObjectType")]
    public class SpawnObjectType: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SpawnObjectType()
        {
        }

        [ExplicitKey]
        public int SpawnObjectTypeID { get; set; }
        public string Name { get; set; }
    }
}
