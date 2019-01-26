
using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SpaceEncounter]")]
    public class SpaceEncounter: IEntity
    {
        [ExplicitKey]
        public string Planet { get; set; }
        public int Type { get; set; }
        public int Chance { get; set; }
        public int Difficulty { get; set; }
    }
}
