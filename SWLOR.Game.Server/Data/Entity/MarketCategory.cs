using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class MarketCategory: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
