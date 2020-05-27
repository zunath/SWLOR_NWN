using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.CraftingDevice
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
            // Clean up temporary data and return all items placed inside.
            NWPlayer player = (_.GetLastClosedBy());
            NWPlaceable device = (_.OBJECT_SELF);
            var model = CraftService.GetPlayerCraftingData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            model.IsAccessingStorage = false;

            foreach (var item in model.MainComponents)
            {
                _.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }
            foreach (var item in model.SecondaryComponents)
            {
                _.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }
            foreach (var item in model.TertiaryComponents)
            {
                _.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }
            foreach (var item in model.EnhancementComponents)
            {
                _.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }

            _.SetEventScript(device.Object, EventScript.Placeable_OnUsed, "script_1");
            _.SetEventScript(device.Object, EventScript.Placeable_OnOpen, string.Empty);
            _.SetEventScript(device.Object, EventScript.Placeable_OnClosed, string.Empty);
            _.SetEventScript(device.Object, EventScript.Placeable_OnInventoryDisturbed, string.Empty);
            player.Data.Remove("CRAFTING_MODEL");
        }
    }
}
