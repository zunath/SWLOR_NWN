using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SpawnObjectType]")]
    public class SpawnObjectType: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
