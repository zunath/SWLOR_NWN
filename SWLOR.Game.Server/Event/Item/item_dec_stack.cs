using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class item_dec_stack
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            NWItem item = NWGameObject.OBJECT_SELF;

            // We ignore any decrements to shurikens, darts, and throwing axes.
            if(item.BaseItemType == _.BASE_ITEM_SHURIKEN ||
               item.BaseItemType == _.BASE_ITEM_DART ||
               item.BaseItemType == _.BASE_ITEM_THROWINGAXE)
            {
                NWNXEvents.SkipEvent();
            }

            MessageHub.Instance.Publish(new OnItemDecrementStack());
        }
    }
}