
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ComponentTypes")]
    public class ComponentType: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ComponentType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ComponentTypeID { get; set; }
        public string Name { get; set; }
    
    }
}
