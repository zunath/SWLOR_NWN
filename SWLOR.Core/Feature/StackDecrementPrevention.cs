using SWLOR.Core.NWNX;
using SWLOR.Core.NWScript.Enum.Item;

namespace SWLOR.Core.Feature
{
    public static class StackDecrementPrevention
    {
        /// <summary>
        /// When a throwing item (shuriken, dart, throwing axe) is thrown, prevent the stack from decrementing.
        /// </summary>
        [NWNEventHandler("item_dec_bef")]
        public static void PreventStackDecrement()
        {
            var item = OBJECT_SELF;
            if (!GetIsObjectValid(item)) return;
            var itemType = GetBaseItemType(item);

            // We ignore any decrements to shurikens, darts, and throwing axes.
            if (itemType == BaseItem.Shuriken ||
                itemType == BaseItem.Dart ||
                itemType == BaseItem.ThrowingAxe)
            {
                EventsPlugin.SkipEvent();
            }
        }
    }
}
