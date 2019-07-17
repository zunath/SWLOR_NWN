using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[LootTableItem]")]
    public class LootTableItem: IEntity
    {
        public LootTableItem()
        {
            SpawnRule = "";
        }

        [Key]
        public int ID { get; set; }
        public int LootTableID { get; set; }
        public string Resref { get; set; }
        public int MaxQuantity { get; set; }
        public byte Weight { get; set; }
        public bool IsActive { get; set; }
        public string SpawnRule { get; set; }

        public IEntity Clone()
        {
            return new LootTableItem
            {
                ID = ID,
                LootTableID = LootTableID,
                Resref = Resref,
                MaxQuantity = MaxQuantity,
                Weight = Weight,
                IsActive = IsActive,
                SpawnRule = SpawnRule
            };
        }
    }
}
