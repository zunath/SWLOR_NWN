using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Shared.Domain.Inventory.Contracts
{
    public interface IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems();
    }
}
