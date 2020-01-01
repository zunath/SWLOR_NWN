using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class Spawn: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public int SpawnObjectTypeID { get; set; }
    }
}
