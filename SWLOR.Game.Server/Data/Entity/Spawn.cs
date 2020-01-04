using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Data.Entity
{
    public class Spawn: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public ObjectType SpawnObjectTypeID { get; set; }
    }
}
