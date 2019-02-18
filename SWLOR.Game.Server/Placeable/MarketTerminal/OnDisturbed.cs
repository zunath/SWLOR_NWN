using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IMarketService _market;
        private readonly IItemService _item;
        private readonly IDialogService _dialog;

        public OnDisturbed(
            INWScript script, 
            IMarketService market,
            IItemService item,
            IDialogService dialog)
        {
            _ = script;
            _market = market;
            _item = item;
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            int type = _.GetInventoryDisturbType();

            if (type == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                HandleRemoveItem();
            }
            else if (type == INVENTORY_DISTURB_TYPE_ADDED)
            {
                HandleAddItem();
            }
            return true;
        }

        private void HandleAddItem()
        {
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            NWPlaceable device = Object.OBJECT_SELF;

            _item.ReturnItem(player, item);
        }

        private void HandleRemoveItem()
        {
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            NWPlaceable device = Object.OBJECT_SELF;
            var model = _market.GetPlayerMarketData(player);

            // Done previewing an item. Return to menu.
            if (item.Resref == "exit_preview")
            {
                item.Destroy();
                device.DestroyAllInventoryItems();
                device.IsLocked = false;
                model.IsAccessingInventory = false;
                model.ReturningFromItemPreview = true;
                _dialog.StartConversation(player, device, "MarketTerminal");
                return;
            }

        }

    }
}
