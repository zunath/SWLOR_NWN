using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Inventory.EventHandlers
{
    internal class InventoryEventHandlers
    {
        private IGuiService _gui;

        public InventoryEventHandlers(IGuiService gui)
        {
            _gui = gui;
        }

        [ScriptHandler<OnModuleEquip>]
        public void RefreshOnEquip()
        {
            var player = GetPCItemLastEquippedBy();
            if (!GetIsPC(player))
                return;

            DelayCommand(0.1f, () => _gui.PublishRefreshEvent(player, new EquipItemRefreshEvent()));
        }

        [ScriptHandler<OnModuleUnequip>]
        public void RefreshOnUnequip()
        {
            var player = GetPCItemLastUnequippedBy();
            if (!GetIsPC(player))
                return;

            DelayCommand(0.1f, () => _gui.PublishRefreshEvent(player, new UnequipItemRefreshEvent()));
        }

    }
}
