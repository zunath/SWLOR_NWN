using SWLOR.Shared.Core.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems();
    }
}
