
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DMRoleDomain]")]
    public class DMRoleDomain: IEntity
    {
        [ExplicitKey]
        public int DMRoleDomainID { get; set; }
        public string Description { get; set; }
    }
}
