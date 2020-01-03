using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Placeable.CraftingDevice
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
            NWPlaceable device = (NWGameObject.OBJECT_SELF);
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

            _.SetEventScript(device.Object, EventScriptPlaceable.OnUsed, "script_1");
            _.SetEventScript(device.Object, EventScriptPlaceable.OnOpen, string.Empty);
            _.SetEventScript(device.Object, EventScriptPlaceable.OnClosed, string.Empty);
            _.SetEventScript(device.Object, EventScriptPlaceable.OnInventoryDisturbed, string.Empty);
            player.Data.Remove("CRAFTING_MODEL");
        }
    }
}
