
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("FameRegions")]
    public class FameRegion: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FameRegion()
        {
        }

        [ExplicitKey]
        public int FameRegionID { get; set; }
        public string Name { get; set; }
    }
}
