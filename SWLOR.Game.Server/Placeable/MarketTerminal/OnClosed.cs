using System;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnClosed: IRegisteredEvent
    {
        
        private readonly IMarketService _market;

        public OnClosed(
            
            IMarketService market)
        {
            
            _market = market;
        }

        public bool Run(params object[] args)
        {
            // Should only fire when a player walks away from the device.
            // Clean up temporary data.
            NWPlayer player = _.GetLastClosedBy();
            NWPlaceable device = Object.OBJECT_SELF;
            var model = _market.GetPlayerMarketData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_USED, "jvm_script_1");
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_OPEN, string.Empty);
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_CLOSED, string.Empty);
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, string.Empty);
            
            // Only wipe the data if we're not returning from an item preview for item picking.
            if(!model.IsReturningFromItemPreview &&
               !model.IsReturningFromItemPicking)
                _market.ClearPlayerMarketData(player);
            return true;
        }
    }
}
