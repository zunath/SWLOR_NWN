using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class item_dec_stack
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public void Main()
        {
            if (NWGameObject.OBJECT_SELF == null) return;

            NWItem item = NWGameObject.OBJECT_SELF;
            if (!item.IsValid) return;

            // We ignore any decrements to shurikens, darts, and throwing axes.
            if(item.BaseItemType == BaseItemType.Shuriken ||
               item.BaseItemType == BaseItemType.Dart ||
               item.BaseItemType == BaseItemType.ThrowingAxe)
            {
                NWNXEvents.SkipEvent();
            }

            MessageHub.Instance.Publish(new OnItemDecrementStack(), false);
        }
    }
}