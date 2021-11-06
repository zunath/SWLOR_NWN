using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item;
using SWLOR.Game.Server.Service;
using System.Linq;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Scripts.Door
{
    public class OnOpenKeyCheck : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = GetLastOpenedBy();
            var door = OBJECT_SELF;
            var keyTag = GetLocalString(door, "REQ_KEY_TAG");
            int highestMatchingKeyTier = 0;

            if (!string.IsNullOrEmpty(keyTag))
            {
                var key = player.InventoryItems.ToList().Find(i => i.Tag == keyTag);
                if (key != null)
                {
                    highestMatchingKeyTier = key.ItemProperties.ToList().Aggregate(0, (currentHighest, iprop) => 
                    {
                        var propTypeID = GetItemPropertyType(iprop);
                        if (propTypeID == ItemPropertyType.ACBonus)
                        {
                            var keyTier = GetItemPropertyCostTableValue(iprop);
                            return keyTier > currentHighest ? keyTier : currentHighest;
                        }

                        return currentHighest;
                    });    
                }
            }

            var expectedKeyTier = GetLocalInt(door, "REQ_KEY_TIER");

            // if user not allowed, close the door and optionally send a message
            if (highestMatchingKeyTier < expectedKeyTier)
            {
                ActionCloseDoor(door);
                var msg = GetLocalString(door, "REQ_KEY_FAIL_MSG");
                if (!string.IsNullOrEmpty(msg)) SpeakString(msg);
            }
        }
    }
}
