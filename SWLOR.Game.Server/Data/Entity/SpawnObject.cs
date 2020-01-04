using SWLOR.Game.Server.AI;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Data.Entity
{
    public class SpawnObject: IEntity
    {
        public SpawnObject()
        {
            SpawnRule = "";
            BehaviourScript = "";
        }

        [Key]
        public int ID { get; set; }
        public int SpawnID { get; set; }
        public string Resref { get; set; }
        public int Weight { get; set; }
        public string SpawnRule { get; set; }
        public NPCGroup? NPCGroupID { get; set; }
        public string BehaviourScript { get; set; }
        public Vfx DeathVFXID { get; set; }
        public AIFlags AIFlags { get; set; }
    }
}
