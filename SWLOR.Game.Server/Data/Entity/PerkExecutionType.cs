
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PerkExecutionTypes]")]
    public class PerkExecutionType: IEntity
    {
        public PerkExecutionType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int PerkExecutionTypeID { get; set; }
        public string Name { get; set; }
    }
}
