using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Event.Item
{
    public static class ItemEvents
    {
        public static void DecrementStack()
        {
            var item = OBJECT_SELF;
            if (!GetIsObjectValid(item)) return;
            var itemType = GetBaseItemType(item);

            // We ignore any decrements to shurikens, darts, and throwing axes.
            if (itemType == BaseItem.Shuriken ||
                itemType == BaseItem.Dart ||
                itemType == BaseItem.ThrowingAxe)
            {
                Events.SkipEvent();
            }

            MessageHub.Instance.Publish(new OnItemDecrementStack(), false);
        }

        public static void UseItemAfter()
        {
            // Already handled in the item_use_before script. No need for anything else to run at this point.
            Events.SkipEvent();
        }

        public static void UseItemBefore()
        {
            MessageHub.Instance.Publish(new OnItemUsed());
        }
    }
}
