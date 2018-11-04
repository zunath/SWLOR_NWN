
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[EnmityAdjustmentRule]")]
    public class EnmityAdjustmentRule: IEntity
    {
        public EnmityAdjustmentRule()
        {
            Name = "";
        }

        [ExplicitKey]
        public int EnmityAdjustmentRuleID { get; set; }
        public string Name { get; set; }
    }
}
