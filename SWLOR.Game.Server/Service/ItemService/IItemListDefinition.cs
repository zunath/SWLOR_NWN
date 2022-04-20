using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ItemService
{
    public interface IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems();
    }
}
