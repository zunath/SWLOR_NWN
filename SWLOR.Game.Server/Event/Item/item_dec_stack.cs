using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;

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
            NWItem item = NWScript.OBJECT_SELF;
            if (!item.IsValid) return;

            // We ignore any decrements to shurikens, darts, and throwing axes.
            if(item.BaseItemType == BaseItem.Shuriken ||
               item.BaseItemType == BaseItem.Dart ||
               item.BaseItemType == BaseItem.ThrowingAxe)
            {
                Events.SkipEvent();
            }

            MessageHub.Instance.Publish(new OnItemDecrementStack(), false);
        }
    }
}