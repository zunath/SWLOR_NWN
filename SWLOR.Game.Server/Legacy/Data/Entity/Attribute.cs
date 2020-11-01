using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("Attribute")]
    public class Attribute: IEntity
    {
        public Attribute()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public int NWNValue { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new Attribute
            {
                ID = ID,
                NWNValue = NWNValue,
                Name = Name
            };
        }
    }
}
