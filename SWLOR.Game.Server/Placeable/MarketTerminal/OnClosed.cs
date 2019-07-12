using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            // Should only fire when a player walks away from the device.
            // Clean up temporary data.
            NWPlayer player = _.GetLastClosedBy();
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            var model = MarketService.GetPlayerMarketData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_USED, "script_1");
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_OPEN, string.Empty);
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_CLOSED, string.Empty);
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, string.Empty);
            
            // Only wipe the data if we're not returning from an item preview for item picking.
            if(!model.IsReturningFromItemPreview &&
               !model.IsReturningFromItemPicking)
                MarketService.ClearPlayerMarketData(player);
            return true;
        }
    }
}
