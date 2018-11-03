
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("LootTableItems")]
    public partial class LootTableItem: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LootTableItem()
        {
            this.SpawnRule = "";
        }

        [Key]
        public int LootTableItemID { get; set; }
        public int LootTableID { get; set; }
        public string Resref { get; set; }
        public int MaxQuantity { get; set; }
        public byte Weight { get; set; }
        public bool IsActive { get; set; }
        public string SpawnRule { get; set; }
    
        public virtual LootTable LootTable { get; set; }
    }
}
