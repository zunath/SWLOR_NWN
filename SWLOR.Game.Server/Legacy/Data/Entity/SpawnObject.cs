using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.AI;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("SpawnObject")]
    public class SpawnObject: IEntity
    {
        public SpawnObject()
        {
            SpawnRule = "";
            BehaviourScript = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public int SpawnID { get; set; }
        public string Resref { get; set; }
        public int Weight { get; set; }
        public string SpawnRule { get; set; }
        public int? NPCGroupID { get; set; }
        public string BehaviourScript { get; set; }
        public int DeathVFXID { get; set; }
        public AIFlags AIFlags { get; set; }

        public IEntity Clone()
        {
            return new SpawnObject
            {
                ID = ID,
                SpawnID = SpawnID,
                Resref = Resref,
                Weight = Weight,
                SpawnRule = SpawnRule,
                NPCGroupID = NPCGroupID,
                BehaviourScript = BehaviourScript,
                DeathVFXID = DeathVFXID,
                AIFlags = AIFlags
            };
        }
    }
}
