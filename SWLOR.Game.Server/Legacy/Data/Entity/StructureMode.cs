using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("StructureMode")]
    public class StructureMode: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new StructureMode
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
