using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Placeable.MarketTerminal
{
    public class OnClosed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            // Should only fire when a player walks away from the device.
            // Clean up temporary data.
            NWPlayer player = _.GetLastClosedBy();
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            var model = MarketService.GetPlayerMarketData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            
            _.SetEventScript(device.Object, EventScriptPlaceable.OnUsed, "script_1");
            _.SetEventScript(device.Object, EventScriptPlaceable.OnOpen, string.Empty);
            _.SetEventScript(device.Object, EventScriptPlaceable.OnClosed, string.Empty);
            _.SetEventScript(device.Object, EventScriptPlaceable.OnInventoryDisturbed, string.Empty);
            
            // Only wipe the data if we're not returning from an item preview for item picking.
            if(!model.IsReturningFromItemPreview &&
               !model.IsReturningFromItemPicking)
                MarketService.ClearPlayerMarketData(player);
        }
    }
}
