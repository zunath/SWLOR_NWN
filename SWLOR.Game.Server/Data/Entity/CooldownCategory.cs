
using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CooldownCategory]")]
    public class CooldownCategory: IEntity
    {
        public CooldownCategory()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public double BaseCooldownTime { get; set; }
    }
}
