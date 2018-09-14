namespace SWLOR.Game.Server.Data.Entities
{
    using System.ComponentModel.DataAnnotations;

    public partial class SpawnObject
    {
        public int SpawnObjectID { get; set; }

        public int SpawnID { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        public int Weight { get; set; }

        public string SpawnRule { get; set; }

        public int? NPCGroupID { get; set; }

        public virtual Spawn Spawn { get; set; }
    }
}
