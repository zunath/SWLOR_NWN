using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidInventory
    {
        public Dictionary<InventorySlot, string> EquippedItems { get; set; }
        public Dictionary<string, string> Inventory { get; set; }

        public DroidInventory()
        {
            EquippedItems = new Dictionary<InventorySlot, string>();
            Inventory = new Dictionary<string, string>();
        }
    }
}
