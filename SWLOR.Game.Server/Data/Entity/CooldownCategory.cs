using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class CooldownCategory: IEntity
    {
        public CooldownCategory()
        {
            Name = "";
        }

        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public double BaseCooldownTime { get; set; }
    }
}
