using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Inventory.Feature
{
    public class StackDecrementPrevention
    {
        private readonly IEventsPluginService _eventsPlugin;

        public StackDecrementPrevention(
            IEventsPluginService eventsPlugin,
            IEventAggregator eventAggregator)
        {
            _eventsPlugin = eventsPlugin;

            // Subscribe to events
            eventAggregator.Subscribe<OnItemDecrementBefore>(e => PreventStackDecrement());
        }

        /// <summary>
        /// When a throwing item (shuriken, dart, throwing axe) is thrown, prevent the stack from decrementing.
        /// </summary>
        public void PreventStackDecrement()
        {
            var item = OBJECT_SELF;
            if (!GetIsObjectValid(item)) return;
            var itemType = GetBaseItemType(item);

            // We ignore any decrements to shurikens, darts, and throwing axes.
            if (itemType == BaseItemType.Shuriken ||
                itemType == BaseItemType.Dart ||
                itemType == BaseItemType.ThrowingAxe)
            {
                _eventsPlugin.SkipEvent();
            }
        }
    }
}
