
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Plants]")]
    public class Plant: IEntity
    {
        public Plant()
        {
            Name = "";
            Resref = "";
            SeedResref = "";
        }

        [ExplicitKey]
        public int PlantID { get; set; }
        public string Name { get; set; }
        public int BaseTicks { get; set; }
        public string Resref { get; set; }
        public int WaterTicks { get; set; }
        public int Level { get; set; }
        public string SeedResref { get; set; }
    }
}
