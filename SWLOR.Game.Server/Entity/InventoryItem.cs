namespace SWLOR.Game.Server.Entity
{
    public class InventoryItem : EntityBase
    {
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public string Tag { get; set; }
        [Indexed]
        public string Resref { get; set; }
        public int Quantity { get; set; }
        public string Data { get; set; }
        public override string KeyPrefix => "InventoryItem";
    }
}
