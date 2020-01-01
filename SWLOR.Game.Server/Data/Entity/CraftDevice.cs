using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class CraftDevice: IEntity
    {
        public CraftDevice()
        {
            Name = "";
        }

        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
