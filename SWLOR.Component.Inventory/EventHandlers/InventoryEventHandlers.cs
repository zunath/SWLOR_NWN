using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Inventory.EventHandlers
{
    internal class InventoryEventHandlers
    {
        private IGuiService _gui;

        public InventoryEventHandlers(
            IGuiService gui,
            IEventAggregator eventAggregator)
        {
            _gui = gui;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEquip>(e => RefreshOnEquip());
            eventAggregator.Subscribe<OnModuleUnequip>(e => RefreshOnUnequip());
        }
        public void RefreshOnEquip()
        {
            var player = GetPCItemLastEquippedBy();
            if (!GetIsPC(player))
                return;

            DelayCommand(0.1f, () => _gui.PublishRefreshEvent(player, new EquipItemRefreshEvent()));
        }
        public void RefreshOnUnequip()
        {
            var player = GetPCItemLastUnequippedBy();
            if (!GetIsPC(player))
                return;

            DelayCommand(0.1f, () => _gui.PublishRefreshEvent(player, new UnequipItemRefreshEvent()));
        }

    }
}
