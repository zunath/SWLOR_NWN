using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Inventory.Feature
{
    public class InstantItemUse
    {
        private readonly IEventsPluginService _eventsPlugin;

        public InstantItemUse(
            IEventsPluginService eventsPlugin,
            IEventAggregator eventAggregator)
        {
            _eventsPlugin = eventsPlugin;

            // Subscribe to events
            eventAggregator.Subscribe<OnItemUseBefore>(e => OnUseItem());
        }

        /// <summary>
        /// Before an item is used, if the item has a script specified, it will be run instantly.
        /// This will bypass the "Use Item" animation items normally have.
        /// </summary>
        public void OnUseItem()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(_eventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            var script = GetLocalString(item, "SCRIPT");

            // No script associated. Let it run the normal execution process.
            if (string.IsNullOrWhiteSpace(script)) return;

            _eventsPlugin.SkipEvent();
            _eventsPlugin.SetEventResult("0"); // Prevents the "You cannot use that item" error message from being sent.
            ExecuteNWScript(script, creature);
        }
    }
}
