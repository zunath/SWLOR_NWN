using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("BuildingTypes")]
    public class BuildingType: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BuildingType()
        {
        }

        [ExplicitKey]
        public int BuildingTypeID { get; set; }
        public string Name { get; set; }
   
    }
}
