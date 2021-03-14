using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ItemModService
{
    public interface IItemModListDefinition
    {
        public Dictionary<string, ItemModDetail> BuildItemMods();
    }
}
