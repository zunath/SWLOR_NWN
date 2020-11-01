using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("CraftDevice")]
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
