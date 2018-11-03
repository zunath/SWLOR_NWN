
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCBaseTypes")]
    public class PCBaseType: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCBaseType()
        {
        }

        [ExplicitKey]
        public int PCBaseTypeID { get; set; }
        public string Name { get; set; }
    }
}
