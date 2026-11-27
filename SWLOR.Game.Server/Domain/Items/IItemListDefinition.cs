using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Items
{
    public interface IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems();
    }
}
