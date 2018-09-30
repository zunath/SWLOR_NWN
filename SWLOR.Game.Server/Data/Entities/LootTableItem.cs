using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class LootTableItem
    {
        public int LootTableItemID { get; set; }

        public int LootTableID { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        public int MaxQuantity { get; set; }

        public byte Weight { get; set; }

        public bool IsActive { get; set; }

        [StringLength(64)]
        public string SpawnRule { get; set; }

        public virtual LootTable LootTable { get; set; }
    }
}
