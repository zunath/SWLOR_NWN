using SWLOR.Component.Inventory.Model;

namespace SWLOR.Component.Inventory.Contracts
{
    public interface IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems();
    }
}
