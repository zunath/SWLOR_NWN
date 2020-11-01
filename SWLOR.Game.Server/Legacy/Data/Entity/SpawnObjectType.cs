using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("SpawnObjectType")]
    public class SpawnObjectType: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new SpawnObjectType
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
