using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Banks]")]
    public class Bank: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string AreaName { get; set; }
        public string AreaTag { get; set; }
        public string AreaResref { get; set; }
    }
}
