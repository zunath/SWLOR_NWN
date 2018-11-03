
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("SpawnObjects")]
    public partial class SpawnObject: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SpawnObject()
        {
            this.SpawnRule = "";
            this.BehaviourScript = "";
        }

        [Key]
        public int SpawnObjectID { get; set; }
        public int SpawnID { get; set; }
        public string Resref { get; set; }
        public int Weight { get; set; }
        public string SpawnRule { get; set; }
        public Nullable<int> NPCGroupID { get; set; }
        public string BehaviourScript { get; set; }
        public int DeathVFXID { get; set; }
    
        public virtual NPCGroup NPCGroup { get; set; }
        public virtual Spawn Spawn { get; set; }
    }
}
