
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("EnmityAdjustmentRule")]
    public class EnmityAdjustmentRule: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EnmityAdjustmentRule()
        {
            Name = "";
        }

        [ExplicitKey]
        public int EnmityAdjustmentRuleID { get; set; }
        public string Name { get; set; }
    }
}
