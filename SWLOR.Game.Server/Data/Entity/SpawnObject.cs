
using System;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SpawnObject]")]
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
    }
}
