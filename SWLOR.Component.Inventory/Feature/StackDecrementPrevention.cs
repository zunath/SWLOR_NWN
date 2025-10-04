using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Inventory.Feature
{
    public class StackDecrementPrevention
    {
        private readonly IEventsPluginService _eventsPlugin;

        public StackDecrementPrevention(IEventsPluginService eventsPlugin)
        {
            _eventsPlugin = eventsPlugin;
        }

        /// <summary>
        /// When a throwing item (shuriken, dart, throwing axe) is thrown, prevent the stack from decrementing.
        /// </summary>
        [ScriptHandler<OnItemDecrementBefore>]
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
