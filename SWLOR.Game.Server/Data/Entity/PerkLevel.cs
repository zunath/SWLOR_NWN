using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PerkLevel: IEntity
    {
        public PerkLevel()
        {
            Description = "";
        }

        [Key]
        public int ID { get; set; }
        public int PerkID { get; set; }
        public int Level { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }
        public int SpecializationID { get; set; }
    }
}
