using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.CraftingDevice
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
            NWPlayer player = (NWScript.GetLastClosedBy());
            NWPlaceable device = (NWScript.OBJECT_SELF);
            var model = CraftService.GetPlayerCraftingData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            model.IsAccessingStorage = false;

            foreach (var item in model.MainComponents)
            {
                NWScript.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }
            foreach (var item in model.SecondaryComponents)
            {
                NWScript.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }
            foreach (var item in model.TertiaryComponents)
            {
                NWScript.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }
            foreach (var item in model.EnhancementComponents)
            {
                NWScript.CopyItem(item.Object, player.Object, true);
                item.Destroy();
            }

            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnUsed, "script_1");
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnOpen, string.Empty);
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnClosed, string.Empty);
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnInventoryDisturbed, string.Empty);
            player.Data.Remove("CRAFTING_MODEL");
        }
    }
}
