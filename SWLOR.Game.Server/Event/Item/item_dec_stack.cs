﻿using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum.Item;
using SWLOR.Game.Server.NWNX;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class item_dec_stack
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            NWItem item = _.OBJECT_SELF;
            if (!item.IsValid) return;

            // We ignore any decrements to shurikens, darts, and throwing axes.
            if(item.BaseItemType == BaseItem.Shuriken ||
               item.BaseItemType == BaseItem.Dart ||
               item.BaseItemType == BaseItem.ThrowingAxe)
            {
                NWNXEvents.SkipEvent();
            }

            MessageHub.Instance.Publish(new OnItemDecrementStack(), false);
        }
    }
}