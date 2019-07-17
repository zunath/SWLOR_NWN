using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CraftDevice]")]
    public class CraftDevice: IEntity
    {
        public CraftDevice()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new CraftDevice
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
