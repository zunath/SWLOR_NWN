
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PerkExecutionTypes")]
    public class PerkExecutionType: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PerkExecutionType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int PerkExecutionTypeID { get; set; }
        public string Name { get; set; }
    }
}
