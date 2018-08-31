using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.Placeable.CraftingDevice
{
    public class OnClosed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ICraftService _craft;
        
        public OnClosed(INWScript script,
            ICraftService craft)
        {
            _ = script;
            _craft = craft;
        }

        public bool Run(params object[] args)
        {
            // Should only fire when a player walks away from the device.
            // Clean up temporary data and return all items placed inside.
            NWPlayer player = NWPlayer.Wrap(_.GetLastClosedBy());
            NWPlaceable device = NWPlaceable.Wrap(Object.OBJECT_SELF);
            var model = _craft.GetPlayerCraftingData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            model.IsAccessingStorage = false;

            foreach (var item in model.MainComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }
            foreach (var item in model.SecondaryComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }
            foreach (var item in model.TertiaryComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }
            foreach (var item in model.EnhancementComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }

            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_USED, "jvm_script_1");
            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_OPEN, string.Empty);
            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_CLOSED, string.Empty);
            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, string.Empty);
            player.Data.Remove("CRAFTING_MODEL");
            return true;
        }
    }
}
