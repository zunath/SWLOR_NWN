using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.MarketTerminal
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
            NWPlayer player = NWScript.GetLastClosedBy();
            NWPlaceable device = NWScript.OBJECT_SELF;
            var model = MarketService.GetPlayerMarketData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnUsed, "script_1");
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnOpen, string.Empty);
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnClosed, string.Empty);
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnInventoryDisturbed, string.Empty);
            
            // Only wipe the data if we're not returning from an item preview for item picking.
            if(!model.IsReturningFromItemPreview &&
               !model.IsReturningFromItemPicking)
                MarketService.ClearPlayerMarketData(player);
        }
    }
}
