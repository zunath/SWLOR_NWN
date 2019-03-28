using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Plant]")]
    public class Plant: IEntity
    {
        public Plant()
        {
            Name = "";
            Resref = "";
            SeedResref = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public int BaseTicks { get; set; }
        public string Resref { get; set; }
        public int WaterTicks { get; set; }
        public int Level { get; set; }
        public string SeedResref { get; set; }
    }
}
